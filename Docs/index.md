# KdSoft.Lmdb Documentation
A .NET wrapper for  OpenLDAP's [LMDB](https://github.com/LMDB/lmdb) key-value store.
Provides a .NET/C# friendly API and supports zero-copy access to the native library.

## Overview

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
    var keyBytes = BitConverter.GetBytes(1874);
    if (cursor.MoveToKey(keyBytes)) {
        Assert.True(cursor.GetCurrent(out KeyDataPair entry));
        var dataString = Encoding.UTF8.GetString(entry.Data);
        while (cursor.GetNext(...)) {
            //
        }
    }
}

// iterate over key range (using foreach)
using (var cursor = Dbase.OpenCursor(tx)) {
    var startKeyBytes = BitConverter.GetBytes(33);
    Assert.True(cursor.MoveToKey(startKeyBytes));

    var endKeyBytes = BitConverter.GetBytes(99);
    foreach (var entry in cursor.ForwardFromCurrent) {
        // test for end of range (> 0 or >=0)
        if (Dbase.Compare(tx, entry.Key, endKeyBytes) > 0)
            break;

        var ckey = BitConverter.ToInt32(entry.Key);
        var cdata = Encoding.UTF8.GetString(entry.Data);
                        
        Console.WriteLine($"{ckey}: {cdata}");
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

## Tests

On non-Windows platforms, LMDB must already be installed so that it can be looked up
by DLLImport using the platform-typical name.
