// ============================================================================
// HubClient（超級 verbose 版）
// 目的：
//   - 以 TCP 與 ProcessHub 溝通（NDJSON 協定，一行一訊息）。
//   - 連線 → 應用層握手（Hello / HelloAck）→ 背景讀、背景寫（Channel 單寫入者）
//   - 對外提供 Start/Stop/Send 三種命令 API。
// 設計重點：
//   * 非同步工廠 CreateAsync：因建構子不能 async，所以用工廠來做 Connect/Handshake。
//   * 背景 WriterLoop：單一 writer（Channel.SingleReader=true）避免訊息交錯（race）。
//   * 背景 ReaderLoop：逐行讀取並解析成 Telemetry/Log/Exited，再以事件拋出。
//   * DisposeAsync：可取消（_cts）、可等待（_rx/_tx），確保「乾淨收尾」。
//   * 錯誤可觀測：Error/Disconnected 事件，讓呼叫者知道發生什麼事。
// ----------------------------------------------------------------------------
// 高階心智圖：
//
//   呼叫端                   HubClient                                    Hub
//   ───────────┬───────────────┬──────────────────────────────┬───────────────
//              |               |                              |
//        CreateAsync()         |  ConnectAsync → GetStream    |
//              |               |  建立 _r/_w（UTF-8, \n）      |
//              |               |  Write Hello(JSON)           |
//              |               |  Read HelloAck(JSON, timeout)|
//              |               |  啟動 WriterLoop / ReaderLoop|
//              |               |                              |
//   Start/Stop/Send ──→ SendAsyncInternal → Channel → WriterLoop → _w.WriteLineAsync → TCP → Hub
//              |                                              |
//   事件訂閱  ←────────── ReaderLoop ← _r.ReadLineAsync ← TCP ← Hub (Telemetry/Log/Exited)
//              |               |                              |
//        DisposeAsync()        |  _cts.Cancel → 停 writer/reader → Dispose streams & tcp
//
// ----------------------------------------------------------------------------
// 背壓（backpressure）：
//   * WriterLoop 對 _w.WriteLineAsync 使用 await，若對端/OS 緩衝區滿 → Task 未完成 → 呼叫端自然放慢。
//   * Send API 不直接寫 _w，而是丟進 Channel；真正寫入只有單一 writer，避免交錯。
// ----------------------------------------------------------------------------
// 併發（concurrency）：
//   * 單寫者（SingleReader=true）：即使多線程呼叫 Send，寫入順序仍序列化（FIFO）。
//   * 讀取單執行緒：ReaderLoop 唯一消費 _r.ReadLineAsync 的輸入。
// ----------------------------------------------------------------------------
// NDJSON：每行即一個 JSON 物件（\n 結尾）。訊息型別以「嘗試反序列化多型別」來判斷。
// ============================================================================

using Shared.Contracts;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace wzd32.Services
{
    public sealed class HubClient : IAsyncDisposable
    {
        // ---- 永續欄位（物件整生存期存在） ------------------------------------
        private readonly TcpClient _tcp = new(); // TCP 客戶端，承載 NetworkStream

        // ---- 延遲初始化欄位（連線成功後才建立，故 nullable） -------------------
        private StreamReader? _r; // 文字層讀取：NetworkStream → UTF-8 → ReadLineAsync
        private StreamWriter? _w; // 文字層寫入：WriteLineAsync（\n 為行結尾）

        // ---- 取消控制與背景工作 -----------------------------------------------
        private readonly CancellationTokenSource _cts = new(); // 全域取消信號
        private Task? _rx; // 背景讀取任務（ReaderLoop）
        private Task? _tx; // 背景寫入任務（WriterLoop）

        // ---- 單一寫入者佇列 ---------------------------------------------------
        // 用 Channel 來序列化所有輸出，避免多執行緒對 StreamWriter 的競爭與訊息交錯。
        // SingleReader=true：只有一個消費者（WriterLoop）；SingleWriter=false：多處可 TryWrite。
        private readonly Channel<string> _out =
            Channel.CreateUnbounded<string>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        // ---- 對外事件（觀察性） -----------------------------------------------
        // 讀到的 NDJSON 會被解析成 Telemetry/Log/Exited 並以事件告知使用者。
        public event Action<Telemetry>? Telemetry;
        public event Action<Log>? Log;
        public event Action<Exited>? Exited;

        // 連線狀態（EOF/取消/錯誤）與錯誤訊息回報
        public event Action<string>? Disconnected;
        public event Action<Exception>? Error;

        // ---- 建構控制：強制用 CreateAsync 完成非同步初始化 ---------------------
        private HubClient() { }

        // =========================================================================
        // CreateAsync：非同步工廠
        //   - 連線（ConnectAsync）
        //   - 建立 Reader/Writer（UTF-8、\n）
        //   - 應用層握手：送 Hello，等 HelloAck（逾時）
        //   - 啟動 WriterLoop/ReaderLoop
        // =========================================================================
        public static async Task<HubClient> CreateAsync(
            string host = "127.0.0.1",
            int port = 54001,
            string token = "dev-token",
            TimeSpan? helloTimeout = null,
            CancellationToken cancellationToken = default)
        {
            var c = new HubClient();
            try 
            {
                await c.ConnectAndHandshakeAsync(
                    host, port, token,
                    helloTimeout ?? TimeSpan.FromSeconds(5),
                    cancellationToken);
                return c; // 回傳「已完成握手且背景讀寫已啟動」的物件
            }
            catch
            {
                await c.DisposeAsync();
                throw;

            }
           

        }

        // ---- 對外 API：送命令（Start/Stop/Command） ---------------------------
        // 設計：不直接觸碰 _w，改丟進 Channel，由 WriterLoop 單一寫者負責寫出。
        public Task StartAsync(Start s) => SendAsyncInternal(Wire.ToJson(s));
        public Task StopAsync(Stop s) => SendAsyncInternal(Wire.ToJson(s));
        public Task SendAsync(Command c) => SendAsyncInternal(Wire.ToJson(c));

        // =========================================================================
        // DisposeAsync：非同步釋放
        //   - 發出取消（_cts.Cancel）
        //   - 關閉輸出佇列（TryComplete）
        //   - 等待背景任務收尾（_tx/_rx）
        //   - 最後釋放 _w/_r/_tcp
        // =========================================================================
        public async ValueTask DisposeAsync()
        {
            try
            {
                _cts.Cancel();             // 1) 對內發出取消信號（停止讀寫循環）
                _out.Writer.TryComplete(); // 2) 結束輸出佇列，避免新寫入

                if (_tx is not null) await _tx.ConfigureAwait(false); // 3) 等 writer
                if (_rx is not null) await _rx.ConfigureAwait(false); // 4) 等 reader
            }
            catch
            {
                // 忽略取消相關例外，確保 finally 能清理資源
            }
            finally
            {
                // 5) 釋放底層資源（注意 _w/_r 可能為 null）
                try { _w?.Dispose(); } catch { }
                try { _r?.Dispose(); } catch { }
                try { _tcp.Dispose(); } catch { }
            }
        }

        // ==================== 私有輔助：連線＋握手 ==============================

        private async Task ConnectAndHandshakeAsync(
            string host, int port, string token, TimeSpan helloTimeout, CancellationToken externalCt)
        {
            // 將外部取消與內部取消合併，任何一方取消都會中止流程
            using var link = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalCt);
            var ct = link.Token;

            // 1) 連線（非同步，不阻塞呼叫執行緒；支援取消）
            await _tcp.ConnectAsync(host, port, ct).ConfigureAwait(false);

            // 2) 抓 NetworkStream（雙向）
            var ns = _tcp.GetStream();

            // 3) 建 Reader/Writer（UTF-8，\n 為行結尾）
            //    Reader：leaveOpen=true，避免先 Dispose Reader 就把底層 stream 關掉，影響 Writer。
            _r = new StreamReader(ns, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                                  bufferSize: 1024, leaveOpen: true);

            _w = new StreamWriter(ns, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true, // 每次 WriteLine 都直接 flush，避免資料卡在緩衝（NDJSON 需要邊界即時送出）
                NewLine = "\n"  // 與協定一致，行結尾固定 \n
            };

            // 4) 應用層握手（送 Hello）
            await _w.WriteLineAsync(Wire.ToJson(new Hello(Token: token))).ConfigureAwait(false);

            // 5) 等待 HelloAck（握手確認）；加入逾時與取消保護
            using (var ctsHello = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                ctsHello.CancelAfter(helloTimeout); // 逾時時間（預設 5s，可由參數覆蓋）
                string? line;
                try
                {
                    // ReadLineAsync(CancellationToken) → ValueTask<string?>；AsTask() 便於一致的 await 風格
                    line = await _r.ReadLineAsync(ctsHello.Token).AsTask().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 逾時或取消都會拋 OCE；這裡將逾時轉成語意清楚的 TimeoutException
                    throw new TimeoutException("Handshake timed out waiting for HelloAck.");
                }

                if (line is null || Wire.FromJson<HelloAck>(line) is null)
                {
                    // 收到 EOF 或不是 HelloAck → 視為握手失敗
                    throw new InvalidOperationException("Handshake failed: HelloAck not received.");
                }
            }

            // 6) 啟動背景 writer（單讀者）
            //    負責從 Channel 取出字串並以 _w.WriteLineAsync 順序寫出（提供背壓、避免交錯）
            _tx = Task.Run(WriterLoopAsync, _cts.Token);

            // 7) 啟動背景 reader
            //    以 _r.ReadLineAsync 逐行讀入，嘗試反序列化成 Telemetry/Log/Exited，再以事件拋出
            _rx = Task.Run(() => ReaderLoopAsync(_cts.Token), _cts.Token);
        }

        // ==================== 背景 Writer：單寫入者 ==============================
        private async Task WriterLoopAsync()
        {
            try
            {
                // 外層 while：等待佇列有資料可讀；取消時會拋 OCE 中止循環
                while (await _out.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    // 內層 while：把目前累積的所有項目一次讀乾（減少 await 次數）
                    while (_out.Reader.TryRead(out var line))
                    {
                        // 核心 I/O：真正寫入（提供背壓、避免交錯）
                        var w = _w ?? throw new ObjectDisposedException(nameof(HubClient));
                        await w!.WriteLineAsync(line).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常中止（例如 DisposeAsync → _cts.Cancel）
            }
            catch (Exception ex)
            {
                // 任意寫入 I/O 異常 → 對外回報，並取消整體（讓 reader 也停）
                Error?.Invoke(ex);
                _cts.Cancel();
            }
        }

        // ==================== 背景 Reader：逐行解析 ==============================
        private async Task ReaderLoopAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    // one line, NDJSON
                    // or EOF parsed as null
                    var line = await _r!.ReadLineAsync(ct).AsTask().ConfigureAwait(false);
                    if (line is null)
                    { 
                        _out.Writer.TryComplete(); // 停止 WriterLoop
                        _cts.Cancel();          // 停止自己
                        Disconnected?.Invoke("EOF"); // 對端關閉連線
                        break;
                    }

                    // 嘗試依序解析三種已知型別；成功就觸發對應事件
                    if (Wire.FromJson<Telemetry>(line) is { } t) { Telemetry?.Invoke(t); continue; }
                    if (Wire.FromJson<Log>(line) is { } l) { Log?.Invoke(l); continue; }
                    if (Wire.FromJson<Exited>(line) is { } x) { Exited?.Invoke(x); continue; }


                    //未識別訊息：可視需求記錄或忽略（避免打爆 log）
                    Console.WriteLine($"[HubClient] Unknown line: {line}");
                }
            }
            catch (OperationCanceledException)
            {
                Disconnected?.Invoke("Canceled"); // 例如 Dispose 或上層取消
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                Disconnected?.Invoke($"Error: {ex.Message}");
            }
        }

        // ==================== 對外送出（封裝成 Channel 寫入） ===================
        private Task SendAsyncInternal(string line)
        { 
            // 若物件已進入關閉狀態，直接丟例外以避免呼叫端以為成功送出
            if (_cts.IsCancellationRequested)
                throw new ObjectDisposedException(nameof(HubClient));

            // TryWrite：若 writer 已完成（TryComplete 過）會回 false；此時丟語意清楚的例外
            if (!_out.Writer.TryWrite(line))
                throw new InvalidOperationException("Send queue closed.");

            // enqueue 成功 → 立刻回已完成的 Task（真正寫入由 WriterLoop 處理）
            return Task.CompletedTask;
        }
    }
}
