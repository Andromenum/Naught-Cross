mergeInto(LibraryManager.library, {
    SyncFilesToIndexedDB: function () {
        FS.syncfs(false, function (err) {
            if (err) {
                console.error("FS.syncfs failed:", err);
            } else {
                console.log("WebGL filesystem synced to IndexedDB.");
            }
        });
    }
});
