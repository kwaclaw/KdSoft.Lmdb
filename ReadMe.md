## KdSoft.Lmdb

A .NET wrapper for  OpenLDAP's [LMDB](https://github.com/LMDB/lmdb) key-value store. It introduces the use of the `Span<byte>` type for interacting with the native LMDB library to reduce the need for copying byte buffers between native and managed scope.

The native C-API is exposed in a .NET typical way, so its use should be familiar to .NET developers.

It requires the .NET Core SDK 2.1 (or later) installed.

## Code Example

#### Create a database

```c#
var envConfig = new EnvironmentConfiguration(10);
using (var env = new Environment(envConfig)) {
    env.Open(envPath);    
    Database dbase;
    var dbConfig = new DatabaseConfiguration(DatabaseOptions.Create);
    using (var tx = env.BeginDatabaseTransaction(TransactionModes.None)) {
        dbase = tx.OpenDatabase("TestDb1", dbConfig);
        tx.Commit();
    }
    // use dbase from here on
}
```

#### Simple Store and Retrieve

```c#
<Env points to an open Environment handle>
...  
var config = new DatabaseConfiguration(DatabaseOptions.Create);
Database dbase;
using (var tx = Env.BeginDatabaseTransaction(TransactionModes.None)) {
    dbase = tx.OpenDatabase("SimpleStoreRetrieve", config);
    tx.Commit();
}

int key = 234;
var keyBuf = BitConverter.GetBytes(key);
string putData = "Test Data";

using (var tx = Env.BeginTransaction(TransactionModes.None)) {
    dbase.Put(tx, keyBuf, Encoding.UTF8.GetBytes(putData), PutOptions.None);
    tx.Commit();
}

ReadOnlySpan<byte> getData;
using (var tx = Env.BeginReadOnlyTransaction(TransactionModes.None)) {
    Assert.True(dbase.Get(tx, keyBuf, out getData));
    tx.Commit();
}

Assert.Equal(putData, Encoding.UTF8.GetString(getData));
```

#### Cursor Operations - Single-Value Database

```c#
<Dbase points to an open Database handle, tx is an open transaction>
...
// basic iteration
using (var cursor = Dbase.OpenCursor(tx)) {
    foreach (var entry in cursor.Forward) {  // cursor.Reverse goes the other way
        var key = BitConverter.ToInt32(entry.Key);
        var data = Encoding.UTF8.GetString(entry.Data);
    }
}

// move cursor to key position and get data forward from that key
using (var cursor = Dbase.OpenCursor(tx)) {
    var keyBytes = BitConverter.GetBytes(4);
    if (cursor.MoveToKey(keyBytes)) {
        Assert.True(cursor.GetCurrent(out KeyDataPair entry));
        var dataString = Encoding.UTF8.GetString(entry.Data);
        while (cursor.GetNext(...)) {
            //
        }
    }
}
```
#### Cursor Operations - Multi-Value Database
```c#
// iteration over multi-value database
using (var cursor = Dbase.OpenMultiValueCursor(tx)) {
    foreach (var keyEntry in cursor.ForwardByKey) {
        var key = BitConverter.ToInt32(keyEntry.Key);
        var valueList = new List<string>();
        // iterate over the values in the same key
        foreach (var value in cursor.ValuesForward) {
            var data = Encoding.UTF8.GetString(value);
            valueList.Add(data);
        }
    }
}

// move to key, iterate over multiple values for key
using (var cursor = Dbase.OpenMultiValueCursor(tx)) {
    Assert.True(cursor.MoveToKey(BitConverter.GetBytes(234));
    var valueList = new List<string>();
    foreach (var value in cursor.ValuesForward) {
        var data = Encoding.UTF8.GetString(value);
        valueList.Add(data);
    }
}

// Move to key *and* nearest data in multi-value database
using (var cursor = Dbase.OpenMultiValueCursor(tx)) {
    var dataBytes = Encoding.UTF8.GetBytes("Test Data");
    var keyData = new KeyDataPair(BitConverter.GetBytes(4), dataBytes);
    KeyDataPair entry;  // the key-value pair nearest to keyData
    Assert.True(cursor.GetNearest(keyData, out entry));
    var dataString = Encoding.UTF8.GetString(entry.Data);
}
```

The unit tests have more examples, especially for cursor operations.

## Motivation

A short description of the motivation behind the creation and maintenance of the project. This should explain **why** the project exists.

## Installation

Include as Nuget package from https://www.nuget.org/packages/KdSoft.Lmdb/ . This is not quite sufficient on platforms other than Windows:

#### Installing the native libraries
* Windows: A recent x64 build is included in this project.

* Linux-like: 

  * Install package, Example for Ubuntu: `sudo apt-get install liblmdb-dev`
  * Install from source, like in this example

  ```
  git clone https://github.com/LMDB/lmdb
  cd lmdb/libraries/liblmdb
  make && make install
  ```

## API Reference

Depending on the size of the project, if it is small and simple enough the reference docs can be added to the README. For medium size to larger projects it is important to at least provide a link to where the API reference docs live.



The native API is documented here: http://www.lmdb.tech/doc/ .

## Tests

On non-Windows platforms, LMDB must already be installed so that it can be looked up
by DLLImport using the platform-typical name.
