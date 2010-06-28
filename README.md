概要
----
groongaのデータベースでログの保存と参照を行えるようにするTIGアドインです。
現状groongaがLinuxでしか動かないようなのでMono専用。

必要環境
-------
apt-get install mecab
wget http://groonga.org/files/groonga/groonga-0.7.3.tar.gz
tar xzvf groonga-0.7.3.tar.gz
cd groonga-0.7.3
./configure --prefix=/usr/local/groonga && make && make install
export LD_LIBRARY_PATH=/usr/local/groonga/log:$LD_LIBRARY_PATH

使い方
-----
GroongaLoggerコンテキストでEnableすれば使えるようになります。
なにやらよくわからないエラーが出る時はgroongaのライブラリへのパスが通ってないのかも。

