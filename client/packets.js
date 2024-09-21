export const PACKET_TYPES = {
  UNKNOWN: 0,

  // Handled by the server
  RESIZE_SHELL: 3,
  SEND_TO_SHELL: 4,

  // Handled by the client
  SHELL_OPENED: 21,
  SHELL_CLOSED: 22,
  SHELL_DATA: 23
}

/**
 * @param {Uint8Array} bytes
 */
export function parsePacket(bytes) {
  if (bytes.length < 1) {
    throw new Error("Invalid packet.");
  }

  const type = bytes[0];

  switch (type) {
    case PACKET_TYPES.SHELL_OPENED: return new ShellOpenedPacket(bytes);
    case PACKET_TYPES.SHELL_CLOSED: return new ShellClosedPacket(bytes);
    case PACKET_TYPES.SHELL_DATA: return new ShellDataPacket(bytes);
    default: return null;
  }
}

export class ResizeShellPacket {
  constructor(cols, rows, width, height) {
    this.type = PACKET_TYPES.RESIZE_SHELL;

    this.cols = cols;
    this.rows = rows;
    this.width = width;
    this.height = height;

    this.size = 17;
  }

  serialize() {
    const ab = new ArrayBuffer(this.size);
    const view = new DataView(ab);
    view.setUint8(0, this.type);
    view.setUint32(1, this.cols);
    view.setUint32(5, this.rows);
    view.setUint32(9, this.width);
    view.setUint32(13, this.height);
    return ab;
  }
}

export class SendToShellPacket {
  
  /**
  * @param { Uint8Array } data
  */
  constructor(data) {
    this.type = PACKET_TYPES.SEND_TO_SHELL;

    this.data = data;
    
    this.size = 1 + data.byteLength;
  }

  serialize() {
    const ab = new ArrayBuffer(this.size);
    const view = new DataView(ab);
    view.setUint8(0, this.type);

    const dataPart = new Uint8Array(ab, 1, this.data.length);
    dataPart.set(this.data);

    return ab;
  }
}

export class ShellOpenedPacket {
  
  /**
  * @param { Uint8Array } bytes
  */
  constructor(bytes) {
    const size = 1;

    if (bytes.length < size) {
      throw new Error("Packet is invalid.");
    }

    const view = new DataView(bytes.buffer);
    this.type = view.getUint8(0);
    if (this.type !== PACKET_TYPES.SHELL_OPENED) {
      throw new Error(`Packet of type ${this.type} is not ShellOpenedPacket.`);
    }
  }
}

export class ShellClosedPacket {
  /**
  * @param { Uint8Array } bytes
  */
  constructor(bytes) {
    const size = 1;

    if (bytes.length < size) {
      throw new Error("Packet is invalid.");
    }

    const view = new DataView(bytes.buffer);
    this.type = view.getUint8(0);
    if (this.type !== PACKET_TYPES.SHELL_CLOSED) {
      throw new Error(`Packet of type ${this.type} is not ShellClosedPacket.`);
    }
  }
}

export class ShellDataPacket {
  /**
  * @param { Uint8Array } bytes
  */
  constructor(bytes) {
    const headerSize = 1;

    if (bytes.length < headerSize) {
      throw new Error("Packet is invalid.");
    }

    const view = new DataView(bytes.buffer);
    this.type = view.getUint8(0);
    if (this.type !== PACKET_TYPES.SHELL_DATA) {
      throw new Error(`Packet of type ${this.type} is not ShellDataPacket.`);
    }

    this.data = bytes.slice(1);
  }
}
