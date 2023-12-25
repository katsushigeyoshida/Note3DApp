using CoreLib;
using System.Windows;

namespace Note3DApp
{
    /// <summary>
    /// 操作モードのパラメータ
    /// </summary>
    public enum OPEMODE { 
        non, pick, loc, areaDisp, areaPick, clear
    }

    /// <summary>
    /// コマンド処理のパラメータ
    /// </summary>
    public enum OPERATION
    {
        non, loc, pick,
        newModel, newParts, newElement,
        createLine, createArc, createCircle, createRect, createPolygon, createWireCube,
        createCube,
        translate, rotate, mirror, strech, changeProperty,
        infoElemnt,
        back, close
    }

    /// <summary>
    /// コマンドレベル
    /// </summary>
    public enum COMMANDLEVEL
    {
        non, main, sub
    }

    /// <summary>
    /// コマンドの構造体
    /// </summary>
    class Command
    {
        public string mainCommand;
        public string subCommand;
        public OPERATION operation;

        public Command(string main, string sub, OPERATION ope)
        {
            mainCommand = main;
            subCommand = sub;
            operation = ope;
        }
    }

    /// <summary>
    /// コマンドデータ
    /// </summary>
    class CommandData
    {
        public List<Command> mCommandData = new() {
            new Command("新規", "新規",         OPERATION.newModel),
            new Command("追加", "部品",         OPERATION.newParts),
            new Command("追加", "ーー",         OPERATION.non),
            new Command("追加", "線分",         OPERATION.createLine),
            new Command("追加", "円弧",         OPERATION.createArc),
            new Command("追加", "円",           OPERATION.createCircle),
            new Command("追加", "四角",         OPERATION.createRect),
            new Command("追加", "ポリゴン",     OPERATION.createPolygon),
            new Command("追加", "立体枠",       OPERATION.createWireCube),
            new Command("追加", "立方体",       OPERATION.createCube),
            new Command("追加", "戻る",         OPERATION.back),
            new Command("編集", "移動",         OPERATION.translate),
            new Command("編集", "回転",         OPERATION.rotate),
            new Command("編集", "反転",         OPERATION.mirror),
            new Command("編集", "ストレッチ",   OPERATION.strech),
            new Command("編集", "戻る",         OPERATION.back),
            new Command("終了", "終了",         OPERATION.close),
        };

        /// <summary>
        /// メインコマンドリストの取得
        /// </summary>
        /// <returns>コマンドリスト</returns>
        public List<string> getMainCommand()
        {
            List<string> main = new List<string>();
            foreach (var cmd in mCommandData) {
                if (!main.Contains(cmd.mainCommand) && cmd.mainCommand != "")
                    main.Add(cmd.mainCommand);
            }
            return main;
        }

        /// <summary>
        /// サブコマンドリストの取得
        /// </summary>
        /// <param name="main">メインコマンド</param>
        /// <returns>コマンドリスト</returns>
        public List<string> getSubCommand(string main)
        {
            List<string> sub = new List<string>();
            foreach (var cmd in mCommandData) {
                if (cmd.mainCommand == main || cmd.mainCommand == "") {
                    if (!sub.Contains(cmd.subCommand))
                        sub.Add(cmd.subCommand);
                }
            }
            return sub;
        }

        /// <summary>
        /// コマンドレベルの取得
        /// </summary>
        /// <param name="menu">コマンド名</param>
        /// <returns>コマンドレベル</returns>
        public COMMANDLEVEL getCommandLeve(string menu)
        {
            int n = mCommandData.FindIndex(p => p.subCommand == menu);
            if (0 <= n)
                return COMMANDLEVEL.sub;
            n = mCommandData.FindIndex(p => p.mainCommand == menu);
            if (0 <= n)
                return COMMANDLEVEL.main;
            return COMMANDLEVEL.non;
        }

        /// <summary>
        /// コマンドコードの取得
        /// </summary>
        /// <param name="menu">サブコマンド名</param>
        /// <returns>コマンドコード</returns>
        public OPERATION getCommand(string menu)
        {
            if (0 <= mCommandData.FindIndex(p => p.subCommand == menu)) {
                Command com = mCommandData.Find(p => p.subCommand == menu);
                return com.operation;
            }
            return OPERATION.non;
        }
    }

    /// <summary>
    /// コマンド処理
    /// </summary>
    class CommandOpe
    {
        public ModelData mModelData;
        public OPERATION mOperation = OPERATION.non;
        public MainWindow mMainWindow;

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mainWindow">Windowハンドル</param>
        /// <param name="modelData">モデルデータ</param>
        public CommandOpe(MainWindow mainWindow, ModelData modelData)
        {
            mMainWindow = mainWindow;
            mModelData = modelData;
        }

        /// <summary>
        /// コマンドの実行
        /// </summary>
        /// <param name="ope">コマンドコード</param>
        /// <returns>操作モード</returns>
        public OPEMODE execCommand(OPERATION ope)
        {
            mOperation = ope;
            OPEMODE opeMode = OPEMODE.loc;
            switch (ope) {
                case OPERATION.newModel:
                    newModel();
                    opeMode = OPEMODE.clear;
                    break;
                case OPERATION.newParts:
                    newParts();
                    opeMode = OPEMODE.clear;
                    break;
                case OPERATION.newElement:
                    newElement();
                    opeMode = OPEMODE.clear;
                    break;
                case OPERATION.createLine: break;
                case OPERATION.createArc: break;
                case OPERATION.createCircle: break;
                case OPERATION.createRect: break;
                case OPERATION.createPolygon: break;
                case OPERATION.createWireCube: break;
                case OPERATION.createCube: break;
                case OPERATION.back:
                    opeMode = OPEMODE.non;
                    break;
                case OPERATION.translate:break;
                case OPERATION.rotate: break;
                case OPERATION.mirror: break;
                case OPERATION.strech: break;
                case OPERATION.close:
                    opeMode = OPEMODE.non;
                    mMainWindow.Close();
                    break;
                default: opeMode = OPEMODE.non; break;

            }
            return opeMode;
        }

        /// <summary>
        /// プリミティブデータの設定
        /// </summary>
        /// <param name="ope">コマンドコード</param>
        /// <param name="locPos">ロケイトリスト</param>
        /// <param name="last">確定の有無</param>
        /// <returns>実行結果</returns>
        public bool defineData(OPERATION ope, List<PointD> locPos, bool last = false)
        {
            switch (ope) {
                case OPERATION.createLine:
                    if (locPos.Count == 2) {
                        mModelData.addLine(locPos[0], locPos[1]);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createArc:
                    if (locPos.Count == 3) {
                        mModelData.addArc(locPos[0], locPos[2], locPos[1]);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createCircle:
                    if (locPos.Count == 2) {
                        mModelData.addArc(locPos[0], locPos[1]);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createRect:
                    if (locPos.Count == 2) {
                        mModelData.addRect(locPos[0], locPos[1]);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createPolygon:
                    if (1 < locPos.Count && last) {
                        mModelData.addPolygon(locPos);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createWireCube:
                    if (locPos.Count == 2) {
                        double h = locPos[0].length(locPos[1]) / Math.Sqrt(2);
                        mModelData.addWireCube(locPos[0], locPos[1], h);
                        locPos.Clear();
                        return true;
                    }
                    break;
                case OPERATION.createCube:
                    if (locPos.Count == 2) {
                        double h = locPos[0].length(locPos[1]) / Math.Sqrt(2);
                        mModelData.addCube(locPos[0], locPos[1], h);
                        locPos.Clear();
                        return true;
                    }
                    break;
                default: break;
            }
            return false;
        }

        /// <summary>
        /// 全データ削除
        /// </summary>
        public void newModel()
        {
            if (ylib.messageBox(mMainWindow, "すべてのデータを削除します。", "", "確認", MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes) {
                mModelData.mRootParts.clear();
                mModelData.mCurParts = mModelData.mRootParts;
            }
        }

        /// <summary>
        /// カレントにPartsの追加
        /// </summary>
        public void newParts()
        {
            InputBox dlg = new InputBox();
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            dlg.Title = "新規部品名";
            if (dlg.ShowDialog() == true) {
                if (0 < dlg.mEditText.Length)
                    mModelData.addParts(dlg.mEditText);
            }
        }

        /// <summary>
        /// カレントにElementの追加
        /// </summary>
        public void newElement()
        {
            InputBox dlg = new InputBox();
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            dlg.Title = "新規要素名";
            if (dlg.ShowDialog() == true) {
                if (0 < dlg.mEditText.Length)
                    mModelData.addElement(dlg.mEditText);
            }
        }
    }

}
