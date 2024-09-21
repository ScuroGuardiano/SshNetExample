import { FitAddon } from "@xterm/addon-fit";
import { Terminal } from "@xterm/xterm";
import { WebSocketPty } from "./ws-pty";

/**
 * @type { { t: Terminal, f: FitAddon, p: WebSocketPty }[] }
 */
const terms = [];

/**
 * @type { { t: Terminal, f: FitAddon, p: WebSocketPty } }
 */
let activeTerminal = null;

const mainEl = document.querySelector("main");

async function openShell() {
  const host = document.getElementById("host").value;
  const username = document.getElementById("username").value;
  const password = document.getElementById("password").value;

  if (activeTerminal) {
    activeTerminal.t.element.classList.add("hidden");
  }

  const termEl = document.createElement("div");
  termEl.classList.add("terminal");
  mainEl.appendChild(termEl);
  
  const term = new Terminal();
  const fit = new FitAddon();
  term.loadAddon(fit);

  term.open(termEl);
  fit.fit();

  let pty = null;

  try {
    pty = new WebSocketPty("ws://localhost:5002/open-shell", host, username, password, term.cols, term.rows, term.element.clientWidth, term.element.clientHeight);
    await pty.open();
    pty.onData = chunk => term.write(chunk);
    term.onData(chunk => pty.write(chunk));
  }
  catch (err) {
    term.write("Error occured while trying to connect:\n");
    term.write(err.message);
    console.log(err);
  }

  activeTerminal = { t: term, f: fit, p: pty };

  if (pty) {
    pty.onClose = () => closeTerminal(activeTerminal);
  }

  terms.push(activeTerminal);
}

/**
  * @param { { t: Terminal, p: WebSocketPty } } term
  */
function closeTerminal(term) {
  if (term.p && !term.p.closed) {
    term.p.onClose = null;
    term.p.close();
  }

  mainEl.removeChild(term.t.element);
  term.t.dispose();

  terms = terms.filter(t => t != term);
  if (activeTerminal == term) {
    if (terms.length >= 1) {
      activeTerminal(0);
    }
    else {
      activeTerminal = null;
    }
  }
}

function activateTerminal(idx) {
  activeTerminal = terms[idx];
  activeTerminal.t.element.classList.remove("hidden");
}

const openShellBtn = document.querySelector("#open-shell-btn");
const openShellModal = document.querySelector("#open-shell-modal");
const openShellForm = document.querySelector("#open-shell-form");
const openShellCancelBtn = document.querySelector("#cancel-btn");

openShellForm.addEventListener('submit', e => {
  e.preventDefault();
  openShell();
  openShellModal.classList.add("hidden");
});

openShellBtn.addEventListener('click', () => {
  openShellModal.classList.remove("hidden");
});

openShellCancelBtn.addEventListener('click', () => {
  openShellModal.classList.add("hidden");
});

window.addEventListener('resize', () => {
  if (activeTerminal) {
    activeTerminal.f.fit();
    activeTerminal.p.resize(activeTerminal.t.cols, activeTerminal.t.rows, activeTerminal.t.element.clientWidth, activeTerminal.t.element.clientHeight);
  }
});
