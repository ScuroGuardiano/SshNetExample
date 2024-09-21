import { FitAddon } from "@xterm/addon-fit";
import { Terminal } from "@xterm/xterm";
import { WebSocketPty } from "./ws-pty";

var term = new Terminal();
const fit = new FitAddon();
term.loadAddon(fit);

term.open(document.getElementById('terminal'));
fit.fit();

window.addEventListener('resize', fit.fit.bind(fit));

const pty = new WebSocketPty(
  "ws://localhost:5002/open-shell",
  term.cols, term.rows, term.element.clientWidth, term.element.clientHeight
);

pty.onData = chunk => term.write(chunk);
term.onData(chunk => pty.write(chunk));
