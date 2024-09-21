import { SendToShellPacket, ShellClosedPacket, ShellDataPacket, ShellOpenedPacket, parsePacket } from "./packets";

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

    this.ws = new WebSocket(url);
    this.ws.binaryType = "arraybuffer";
    this.ws.addEventListener('message', (event) => {
      if (this.onData) {
        this.processPacket(new Uint8Array(event.data));
      }
    });
  }

  /**
   * @type { (chunk: string | Uint8Array) => void }
   */
  onData = null;

  /**
  * @param { string } chunk
  */
  write(chunk) {
    console.log(`${this.shell}`);
    if (this.ws.readyState === this.ws.OPEN && this.shell) {
      const packet = new SendToShellPacket(this.shell, new TextEncoder().encode(chunk));
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
}
