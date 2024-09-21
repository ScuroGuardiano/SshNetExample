import { ResizeShellPacket, SendToShellPacket, ShellClosedPacket, ShellDataPacket, ShellOpenedPacket, parsePacket } from "./packets";

export class WebSocketPty {
  constructor(address, host, username, password, cols, rows, width, height) {
    this.shell = null;
    const url = new URL(address);
    url.searchParams.set("host", host);
    url.searchParams.set("username", username);
    url.searchParams.set("password", password);
    url.searchParams.set("cols", cols);
    url.searchParams.set("rows", rows);
    url.searchParams.set("width", width);
    url.searchParams.set("height", height);
    this.url = url;
  }

  open() {
    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(this.url);
      this.ws.binaryType = "arraybuffer";
      this.ws.addEventListener('message', (event) => {
        if (this.onData) {
          this.processPacket(new Uint8Array(event.data));
        }
      });
      let errorListener;

      this.ws.addEventListener('open', (event) => {
        this.ws.removeEventListener('error', errorListener);
        resolve(event);
      });

      this.ws.addEventListener('close', () => {
        this.closed = true;
        this.onClose?.();
      });
      this.ws.addEventListener('error', errorListener = (event) => reject(event));
    })
  }

  /**
   * @type { (chunk: string | Uint8Array) => void }
   */
  onData = null;

  /**
   * @type { () => void }
   */
  onClose = null;

  closed = false;

  /**
  * @param { string } chunk
  */
  write(chunk) {
    if (this.ws.readyState === this.ws.OPEN) {
      const packet = new SendToShellPacket(new TextEncoder().encode(chunk));
      this.ws.send(packet.serialize());
    }
  }

  processPacket(bytes) {
    const packet = parsePacket(bytes);

    if (packet == null) {
      console.error("Parsed packet is null :(");
      return;
    }

    if (packet instanceof ShellOpenedPacket) {
      this.shell = packet.shellId;
    }

    if (packet instanceof ShellClosedPacket) {
      this.shell = null;
    }

    if (packet instanceof ShellDataPacket && this.onData) {
      console.log("Data received");
      this.onData(packet.data);
    }
  }

  resize(cols, rows, width, height) {
    if (this.ws.readyState === this.ws.OPEN) {
      const packet = new ResizeShellPacket(cols, rows, width, height);
      this.ws.send(packet.serialize());
    } 
  }

  close() {
    this.ws.close();
    this.closed = true;
  }
}
