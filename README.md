<img src="Nethermind.png" width="600">
Full .NET Core Ethereum client.

[gitter](https://gitter.im/nethermindeth/nethermind?utm_source=share-link&utm_medium=link&utm_campaign=share-link)

[![Build Status](https://travis-ci.org/rlanga/nethermind.svg?branch=master)](https://travis-ci.org/rlanga/nethermind)

## Build (Windows / Linux / MacOS)

### IDE
•	JetBrains Rider https://www.jetbrains.com/rider/<br/>
•	VS Code https://code.visualstudio.com/docs/other/dotnet<br/>

### SDKs
•	Windows https://www.microsoft.com/net/download?initial-os=windows<br/>
•	Linux https://www.microsoft.com/net/download?initial-os=linux (make sure to select the right distribution)<br/>
•	Mac https://www.microsoft.com/net/download?initial-os=macos<br/>

### source and build

```
on Linux:
sudo apt-get update
sudo apt-get install libsnappy-dev
sudo apt-get install libc6-dev
sudo apt-get install libc6

then (any platform):

git clone https://github.com/tkstanczak/nethermind.git
cd nethermind/src/Nethermind
git submodule update --init
dotnet build
```

## mainnet sync (networking is very unstable, current version should not be considered secure or correct, do not use for anything but experimenting with the source code)

//change paths in config.json to run on Linux / MacOS<br/>
//change test node key to something random in config.json<br/>
```
cd src/Nethermind/Nethermind.Runner
dotnet run --config configs//ropsten_windows_discovery.config.json
```

## Contributors welcome
At Nethermind we are building an Open Source multiplatform Ethereum client implementation in .NET Core (running seamlessly both on Linux and Windows). Simultaneously our team works on Nethermind trading tools, analytics and decentralized exchange (0x relay).

Nethermind client can be used in your projects, when setting up private Ethereum networks or dApps. Nethermind is under development and below is the long list of items that are still to be implemented (and we would love to see open source contributions here). A full and up to date list will be maintained via [issues](https://github.com/NethermindEth/nethermind/issues)

### networking / devp2p
1) Implement light client implementation (LES protocol)
2) Implement Warp sync protocol (from Parity - PAR protocol)
3) Reverse engineer and implement discovery v5 protocol from Geth
4) Implement eth63 sync protocol (geth fast sync)

### consensus (both are funded on [Gitcoin](https://gitcoin.co/explorer?network=mainnet&idx_status=open&keywords=Nethermind,nethermind&order_by=-_val_usd_db))
1) Implement Clique (PoA as in Rinkeby by Geth)
2) Implement PoA as in Parity (integrate with Kivan network)

### tools / private network
1) Test sync processes with Hive tests

### store / DB
1) Tune RocksDB to limit memory usage (possibly remove dependency on RocksDB sharp and add our own wrapper around C++ library with PInvoke just for the functions we use)
2) Further improve performance of RLP decoding / encoding by using Recyclable Memory Streams everywhere
3) Implement pruning

### research / future implementations
1) implement sharding
2) implement Casper
3) support plasma cash
4) state channels

# Links
http://nethermind.io/
