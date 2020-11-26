const electron = require('electron');
const url = require('url');
const path = require('path');
const fs = require('fs');
const chokidar = require('chokidar');
const Store = require('electron-store');
const iconv = require("iconv-lite");

const { app, BrowserWindow, clipboard, ipcRenderer } = electron;

const store = new Store();
const c_TextFile = "text.txt";
const c_ImageFile = "img.png";
const c_Encoding = "utf8";
let sharedDir = "F:\\Downloads\\clipboard\\";
let watcher;

function createWindow() {
    const win = new BrowserWindow({
        width: 500,
        height: 125,
        webPreferences: {
            nodeIntegration: true
        },
        resizable: false
    });

    win.removeMenu();

    win.loadURL(url.format({
        pathname: path.join(__dirname, 'index.html'),
        protocol: 'file',
        slashes: true
    }));
}

app.on('ready', function () {
    createWindow();
    const files = [`${sharedDir}${c_TextFile}`, `${sharedDir}${c_ImageFile}`];
    createWatcher(files);
    let raw = fs.readFileSync(files[0], 'utf8');
    let content = iconv.decode(raw, c_Encoding);
    console.log(content);
    startWatching();
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }

    watcher.close();
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});

function createWatcher(files) {
    console.log(`Watching ${files}`);
    watcher = chokidar.watch(files, {
        ignored: /(^|[\/\\])\../, // ignore dotfiles
        persistent: true
    });
}

function startWatching() {
    watcher.on('change', (file) => {
        const filename = path.basename(file);
        switch (filename) {
            case c_TextFile:
                try {
                    let raw = fs.readFileSync(file);
                    let content = iconv.decode(raw, c_Encoding);
                    console.log(content);
                    clipboard.writeText(content);
                } catch (error) {
                    console.log(error);
                }
                break;
            case c_ImageFile:
                break;
        }
    });
}

function stopWatching() {
    watcher.on('change', () => { });
}
