export class DummyPty {
    /**
     * @type { (chunk: string) => void }
     */
    onData = null;

    /**
    * @param { string } chunk
    */
    write(chunk) {
        if (this.onData) {
            this.onData(chunk.replace("\r", "\r\n"));            
        }
    }
}
