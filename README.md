# MultiPlayerMOsample
Use Mirror+ Noble Connect(Relay)

最小限のMirrorを使ったゲーム部分作成。
NAT punchthroughとかはNobleConnectに頼っています。

作った意図としては
- スタート、ロビー、ゲーム、リザルト、　と2シーン以上のシーン構成で動くサンプルを出したかった
- 弾や敵だけじゃなく、アイテムや装備可能オブジェクト、スコア概念を含んだサンプルが欲しかった
- LAN外の回線同士などもリレーサービスを使う事で繋がるサンプルが欲しかった

という感じです。


# プロジェクトの遊び方
Unity2018.3.6f1でプロジェクトを開く(コンパイルエラーが起きます、気にしなくて大丈夫です）

Noble Connect Freeをインポートする
https://assetstore.unity.com/packages/tools/network/noble-connect-free-141599

NobleConnectの設定をReadmeに従って行う

![result](https://github.com/neon-izm/NobleConnectMirrorSample/blob/master/DocImage/Sample.gif?raw=true)



# Environment
- Unity2018.3.6f1
- Noble Connect 1.0.3 https://assetstore.unity.com/packages/tools/network/noble-connect-free-141599
- Mirror ver 3.6.7 https://github.com/vis2k/Mirror/releases/tag/v3.6.7
- LiteNetLib4Mirror-1.1.7 https://github.com/MichalPetryka/LiteNetLib4Mirror/releases/tag/1.1.7

# 現時点の動作確認済み
Assets/NobleConnect/Examples/NetworkManager/ExampleNetworkManager.unity
を利用して別回線同士(光回線とMVNO テザリング)で接続が出来ることを確認しました。(RELAY Modeになってた)

# 最小限に何が分かると良いのか
- 自キャラのロビーとゲームでの差し替え
- 自キャラの移動
- 自キャラパラメータが他クライアントで同期される
- 自キャラから何かをSpawnする（FPSの弾など）
- GameMaster側が何か値を更新して全クライアントで同期する（スコア、あるいは残り時間など）
- GameMaster側が何かのオブジェクトをSpawnする（アイテムなど）
- GameMaster側が何かのNPCをSpawnする（敵相当）
- 自キャラの弾に当たってNPCのHPが減る、0になると死ぬ
- GameMaster側がSpawnしたアイテムを自キャラが装備する（消えない）手放す事も出来る


# Lisence

## LiteNetLib4Mirror
MIT License

Copyright (c) 2019 MichalPetryka

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Mirror
The MIT License (MIT)

Copyright (c) for portions of project Mirror are held by Unity Technologies, 2015 as part of project Unity Technologies Networking (UNET).
All other Copyright (c) for project Mirror are held by vis2k, 2018.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
