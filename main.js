const electron = require('electron');
const url = require('url');
const path = require('path');
const fs = require('fs');
const chokidar = require('chokidar');
const Store = require('electron-store');

const { app, BrowserWindow, clipboard, nativeImage, ipcRenderer } = electron;

const store = new Store();
const c_TextFile = "text.txt";
const c_ImageFile = "img.png";
const c_SaveKeyDir = "path";
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
        pathname: path.join(__dirname, '/dist/index.html'),
        protocol: 'file',
        slashes: true
    }));
}

app.on('ready', function () {
    createWindow();
    const files = [`${sharedDir}${c_TextFile}`, `${sharedDir}${c_ImageFile}`];
    createFileWatcher(files);
    startFileWatching();
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

function createFileWatcher(files) {
    console.log(`Watching ${files}`);
    watcher = chokidar.watch(files, {
        ignored: /(^|[\/\\])\../, // ignore dotfiles
        persistent: true
    });
}

function startFileWatching() {
    watcher.on('change', (file) => {
        const filename = path.basename(file);
        switch (filename) {
            case c_TextFile:
                try {
                    let content = fs.readFileSync(file, c_Encoding);
                    clipboard.writeText(content);
                } catch (error) {
                    console.log(error);
                }
                break;
            case c_ImageFile:
                try {
                    let img = nativeImage.createFromPath(file);
                    clipboard.writeImage(img);
                } catch (error) {
                    console.log(error);
                }
                break;
        }
    });
}

function stopFileWatching() {
    watcher.on('change', () => { });
}

function startClipboardWatching() {

}

function stopClipboardWatching() {

}

function load() {
    sharedDir = store.get(c_SaveKeyDir);
}

function save() {
    store.set(c_SaveKeyDir, sharedDir);
}