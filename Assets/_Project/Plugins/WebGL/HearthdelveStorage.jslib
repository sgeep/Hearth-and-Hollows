// Flushes Unity's persistentDataPath (an in-memory file system on the web) to IndexedDB, so saves survive a reload.
// Called by Hearthdelve.Shared.Save.WebStorage.Flush after each save write and delete.
mergeInto(LibraryManager.library, {
  HearthdelveSyncFiles: function () {
    FS.syncfs(false, function (err) {
      if (err) console.error("Hearthdelve: saving to browser storage failed: " + err);
    });
  },
});
