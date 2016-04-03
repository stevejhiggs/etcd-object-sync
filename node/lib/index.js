'use strict';

const Etcd = require('node-etcd');
let etcd = null;

const get = (key, callback) => {
  etcd.get(key, callback);
};

const set = (key, value) => {
  if (value === undefined) {
    etcd.delete(key);
    return;
  }

  etcd.set(key, JSON.stringify({ value: value }));
};

const sync = (key, syncCallback) => {
  const watcher = etcd.watcher(key, null, {recursive: false});
  watcher.on("set", (value) => {
    syncCallback(null, JSON.parse(value.node.value).value);
  });

  watcher.on("delete", () => {
    syncCallback(null, undefined);
  });

  if (syncCallback) {
    get(key, (err, value) => {
      if (err && err.errorCode === 100) {
        // key does not exist
        return syncCallback(null, undefined);
      }

      syncCallback(err, JSON.parse(value.node.value).value);
    });
  }
};

const config = (optionalEtcd) => {
  if (!etcd) {
    etcd = optionalEtcd || new Etcd();
  }

  return {
    sync: sync,
    set: set
  }
};

module.exports = config;
