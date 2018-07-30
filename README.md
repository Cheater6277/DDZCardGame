# DDZGame

局域网联机斗地主，使用.net framework&amp;WPF实现

## 创意来源

llf0703的类似项目

## 工程进度

正式版发布！

- [x] 主逻辑
- [x] Card结构
- [x] selection结构
- [x] 主逻辑的具体方法实现
- [x] 网络通信模块封装
- [x] 客户端GUI

## 使用帮助

### 安装

双击 `setup.exe` 即可安装。客户端和服务端需要分别安装
在系统 `安装与卸载应用程序` 菜单中即可卸载，在安装新版本之前请卸载旧版本

### 服务端

1. 在开始菜单中打开 `GameServer`
2. 将会自动显示您的IP地址，在客户端输入即可连接
3. 按任意键退出服务器程序

### 客户端

1. 在开始菜单中打开 `GameClient`
2. 点击右上角的菜单键，在弹出的菜单中输入用户名和服务器的IP地址（请确保在同一局域网下）即可连接。程序会记住上一次使用的用户名和地址
3. 如果游戏没有开始，将会显示提示框，点击 `READY` 表示准备好开局，当三个人点击了 `READY` 游戏就会开始，没有准备开局的人只能观战
4. 开局后就会自动发牌，然后自动询问是否叫地主
5. 轮到您出牌时，窗口下方会有提示信息，右上角会有倒计时，点击选择要出的牌，再次点击取消，点击右下角绿色按钮出牌（什么都不选择表示过），如果倒计时结束没有出牌就自动过
6. 右侧边栏会显示出牌记录和聊天记录