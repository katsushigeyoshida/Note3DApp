﻿using CoreLib;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Note3DApp
{
    /// <summary>
    /// TreeView表示用クラス
    /// </summary>
    public class TreeParts
    {
        public string mName { get; set; }
        public List<TreeParts> mParts { get; set; }
        public int mIndex { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;            //  ウィンドウの高さ
        private double mWindowHeight;           //  ウィンドウ幅
        private double mPrevWindowWidth;        //  変更前のウィンドウ幅
        private WindowState mWindowState = WindowState.Normal;  //  ウィンドウの状態(最大化/最小化)

        private string[] mDispFaceMenu = { "XY面", "YZ面", "ZX面", "3D" };
        private double[] mGridSizeMenu = {
            0, 0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 10,
            20, 30, 40, 50, 100, 200, 300, 400, 500, 1000
        };

        private List<PointD> mLocList = new();                  //  ロケイトの保存
        private int mPickBoxSize = 10;                          //  ピック領域サイズ
        private int mMouseScroolSize = 5;                       //  マウスによるスクロール単位

        private OPEMODE mOperationMode = OPEMODE.non;           //  操作モード(loc,pick)
        private Point mPreMousePos;                             //  マウスの前回位置(screen座標)
        private PointD mPrePosition;                            //  マウスの前回位置(world座標)
        private bool mMouseLeftButtonDown = false;              //  左ボタン状態
        private bool mMouseRightButtonDown = false;             //  右ボタン状態

        private DataDrawing mDrawing;
        private ModelData mModelData;
        private CommandData mCommandData;
        private CommandOpe mCommandOpe;
        private FileData mFileData;

        private YLib ylib = new YLib();

        public ObservableCollection<TreeParts> mParts { get; } = new ObservableCollection<TreeParts>();
        Parts parts;

        public MainWindow()
        {
            InitializeComponent();

            mFileData = new FileData(this);

            WindowFormLoad();
        }

        /// <summary>
        /// Windows表示直後の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mDrawing = new DataDrawing(cvCanvas, this);
            mModelData = new ModelData(mDrawing.mGDraw);
            mCommandData = new CommandData();
            mCommandOpe = new CommandOpe(this, mModelData);
            mDrawing.drawWorldFrame();

            lbCommand.ItemsSource = mCommandData.getMainCommand();
            cbDispFace.ItemsSource = mDispFaceMenu;
            cbDispFace.SelectedIndex = 0;

            cbColor.DataContext = ylib.mBrushList;
            cbColor.SelectedIndex = ylib.getBrushNo(mModelData.mPrimitiveBrush);
            cbGridSize.ItemsSource = mGridSizeMenu;
            cbGridSize.SelectedIndex = mGridSizeMenu.FindIndex(Math.Abs(mDrawing.mGridSize));

            mFileData.setBaseDataFolder();
            mCommandOpe.mDataFilePath = mFileData.getCurItemFilePath();
            cbGenre.ItemsSource = mFileData.getGenreList();
            int index = cbGenre.Items.IndexOf(mFileData.mGenreName);
            if (0 <= index) {
                cbGenre.SelectedIndex = index;
                mCommandOpe.loadFile();
            } else {
                if (0 < cbGenre.Items.Count) {
                    mFileData.mGenreName = cbGenre.Items[0].ToString() ?? "";
                    lbItemList.ItemsSource = mFileData.getItemFileList();
                }
            }
            commandClear();
        }

        /// <summary>
        /// Windows 終了時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowFormSave();
        }

        /// <summary>
        /// Windows サイズ変更処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (WindowState != mWindowState && WindowState == WindowState.Maximized) {
                //  ウィンドウの最大化時
                mWindowWidth = SystemParameters.WorkArea.Width;
                mWindowHeight = SystemParameters.WorkArea.Height;
            } else if (WindowState != mWindowState ||
                mWindowWidth != Width || mWindowHeight != Height) {
                //  ウィンドウサイズが変わった時
                mWindowWidth = Width;
                mWindowHeight = Height;
            } else {
                //  ウィンドウサイズが変わらない時は何もしない
                mWindowState = WindowState;
                return;
            }
            mWindowState = WindowState;
            if (mDrawing != null)
                mDrawing.drawWorldFrame();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 ||
                Properties.Settings.Default.MainWindowHeight < 100 ||
                SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MainWindowTop;
                Left = Properties.Settings.Default.MainWindowLeft;
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowHeight;
            }

            if (0 < Properties.Settings.Default.DataFile.Length)
                mFileData.mDataName = Properties.Settings.Default.DataFile;
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            Properties.Settings.Default.DataFile = mFileData.mDataName;

            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = Top;
            Properties.Settings.Default.MainWindowLeft = Left;
            Properties.Settings.Default.MainWindowWidth = Width;
            Properties.Settings.Default.MainWindowHeight = Height;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// キー入力
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            keyCommand(e.Key, e.KeyboardDevice.Modifiers == ModifierKeys.Control, e.KeyboardDevice.Modifiers == ModifierKeys.Shift);
            btDummy.Focus();         //  ダミーでフォーカスを外す
        }

        /// <summary>
        /// マウスの移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(cvCanvas);
            if (pos == mPreMousePos)
                return;
            PointD wpos = mDrawing.screen2World(pos);
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                //  3Dの回転・移動
                mDrawing.mouse3DMove(mModelData.mCurParts, e.GetPosition(cvCanvas));
            } else {
                //  2D表示操作
                if (mMouseLeftButtonDown && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                    //  2Dスクロール
                    if (mMouseScroolSize < ylib.distance(pos, mPreMousePos))
                        scroll(mPreMousePos, pos);
                    else
                        return;
                } else if (0 < mLocList.Count) {
                    //  ドラッギング表示
                    mDrawing.dragging(mCommandOpe.mOperation, mModelData.mPickElement, mLocList, wpos);
                }
            }
            dispStatus(wpos);
            mPreMousePos = pos;     //  スクリーン座標
            mPrePosition = wpos;    //  ワールド座標
        }

        /// <summary>
        /// マウス左ボタンダウン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mMouseLeftButtonDown = true;
            Point pos = e.GetPosition(cvCanvas);
            System.Diagnostics.Debug.WriteLine($"MouseLeftButtonDown {pos.ToString()}");
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                //  3D回転開始
                mDrawing.mouse3DRotateStart(e.GetPosition(cvCanvas));
            } else {
                //  2D表示
                PointD wpos = mDrawing.screen2World(pos);
                if (0 < mDrawing.mGridSize)
                    wpos.round(Math.Abs(mDrawing.mGridSize));
                if (mOperationMode == OPEMODE.loc) {
                    //  ロケイトの追加
                    mLocList.Add(wpos);
                }
                //  データ登録(データ数固定コマンド)
                definData(mCommandOpe.mOperation, mModelData.mCurParts, mLocList);
            }
            dispTitle();
        }

        /// <summary>
        /// マウス左ボタンアップ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                //  3D回転移動終了
                mDrawing.mouse3DEnd();
            } else {
                //  2D表示
                mMouseLeftButtonDown = false;
            }
        }

        /// <summary>
        /// マウス右ボタンダウン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(cvCanvas);
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                //  3D移動開始
                mDrawing.mouse3DTlansLateStart(e.GetPosition(cvCanvas));
            } else {
                //  2D表示
                mMouseRightButtonDown = true;
                PointD wpos = mDrawing.screen2World(pos);
                List<int> picks = getPickNo(wpos);
                if (mOperationMode == OPEMODE.loc) {
                    //  データ登録(データ数不定コマンド)
                    definData(mCommandOpe.mOperation, mModelData.mCurParts, mLocList, true);
                } else {
                    //  ピック色表示
                    for (int i = 0; i < picks.Count; i++) {
                        Element element = mModelData.searchIndexElement(mModelData.mCurParts, picks[i]);
                        if (element != null) {
                            mDrawing.pickPartDraw2D(element);
                            mModelData.addPickElement(element, wpos, mDrawing.mDispMode);
                        }
                    }
                }
            }
            dispTitle();
        }

        /// <summary>
        /// マウス右ボタンアップ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                //  3D回転移動終了
                mDrawing.mouse3DEnd();
            } else {
                //  2D表示
                mMouseRightButtonDown = false;
            }
        }

        /// <summary>
        /// マウスホィール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (0 != e.Delta) {
                if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                    // 3D表示
                    mDrawing.mouse3DScale(mModelData.mCurParts, e.Delta);
                } else {
                    //  2D表示
                    double scaleStep = e.Delta > 0 ? 1.2 : 1 / 1.2;
                    Point pos = e.GetPosition(cvCanvas);
                    PointD wp = mDrawing.screen2World(pos);
                    zoom(wp, scaleStep);
                }
            }
        }

        /// <summary>
        /// コマンド選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbCommand_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = lbCommand.SelectedIndex;
            if (lbCommand.Items != null && 0 <= index) {
                string menu = lbCommand.Items[index].ToString() ?? "";
                COMMANDLEVEL level = mCommandData.getCommandLeve(menu);
                if (level == COMMANDLEVEL.main) {
                    //  メインコマンド
                    lbCommand.ItemsSource = mCommandData.getSubCommand(menu);
                } else if (level == COMMANDLEVEL.sub) {
                    //  サブコマンド
                    OPERATION ope = mCommandData.getCommand(menu);
                    mOperationMode = mCommandOpe.execCommand(ope, mModelData.mPickElement);
                    if (mOperationMode == OPEMODE.clear || mOperationMode == OPEMODE.non) {
                        commandClear();
                        lbCommand.ItemsSource = mCommandData.getMainCommand();
                    }
                }
            }
            dispStatus(null);
            dispTitle();
        }

        /// <summary>
        /// 表示面の切替(XY/YZ/ZX/3D)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDispFace_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= cbDispFace.SelectedIndex) {
                mModelData.mFace = (DISPMODE)Enum.ToObject(typeof(DISPMODE), cbDispFace.SelectedIndex);
                mDrawing.mDispMode = mModelData.mFace;
                mDrawing.partsDraw(mModelData.mCurParts);
            }
        }

        /// <summary>
        /// 色設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= cbColor.SelectedIndex)
                mModelData.mPrimitiveBrush = ylib.mBrushList[cbColor.SelectedIndex].brush;
            btDummy.Focus();         //  ダミーでフォーカスを外す
        }

        /// <summary>
        /// グリッドの設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGridSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 <= cbGridSize.SelectedIndex) {
                mDrawing.mGridSize = mGridSizeMenu[cbGridSize.SelectedIndex];
                mDrawing.partsDraw(mModelData.mCurParts);
            }
        }

        /// <summary>
        /// ジャンルの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGenre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = cbGenre.SelectedIndex;
            if (0 <= index) {
                mFileData.setGenreFolder(cbGenre.SelectedItem.ToString() ?? "");
                lbItemList.ItemsSource = mFileData.getItemFileList();
            }
            dispTitle();
        }

        /// <summary>
        /// モデルデータの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = lbItemList.SelectedIndex;
            if (0 <= index) {
                mCommandOpe.saveFile(true);
                mFileData.mDataName = lbItemList.Items[index].ToString() ?? "";
                mCommandOpe.mDataFilePath = mFileData.getCurItemFilePath();
                mCommandOpe.loadFile();
                commandClear();
            }
            dispTitle();
        }

        /// <summary>
        /// ジャンル選択のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGenreMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("cbGenreAddMenu") == 0) {
                //  大分類(Genre)の追加
                string genre = mFileData.addGenre();
                if (0 < genre.Length) {
                    cbGenre.ItemsSource = mFileData.getGenreList();
                    int index = cbGenre.Items.IndexOf(genre);
                    if (0 <= index)
                        cbGenre.SelectedIndex = index;
                }
            } else if (menuItem.Name.CompareTo("cbGenreRenameMenu") == 0) {
                //  大分類名の変更
                string genre = mFileData.renameGenre(cbGenre.SelectedItem.ToString() ?? "");
                if (0 < genre.Length) {
                    cbGenre.ItemsSource = mFileData.getGenreList();
                    int index = cbGenre.Items.IndexOf(genre);
                    if (0 <= index)
                        cbGenre.SelectedIndex = index;
                }
            } else if (menuItem.Name.CompareTo("cbGenreRemoveMenu") == 0) {
                //  大分類名の削除
                if (mFileData.removeGenre(cbGenre.SelectedItem.ToString() ?? "")) {
                    cbGenre.ItemsSource = mFileData.getGenreList();
                    if (0 < cbGenre.Items.Count)
                        cbGenre.SelectedIndex = 0;
                }
            }
            dispTitle();
        }

        /// <summary>
        /// モデルデータ選択のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbItemMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            string itemName = null;
            if (0 <= lbItemList.SelectedIndex)
                itemName = lbItemList.SelectedItem.ToString() ?? "";

            mCommandOpe.saveFile(true);
            if (menuItem.Name.CompareTo("lbItemAddMenu") == 0) {
                //  図面(Item)の追加
                itemName = mFileData.addItem();
                if (0 < itemName.Length) {
                    mCommandOpe.newModel(true);
                    commandClear();
                    mCommandOpe.mDataFilePath = mFileData.getItemFilePath(itemName);
                    mCommandOpe.saveFile(true);
                    lbItemList.ItemsSource = mFileData.getItemFileList();
                    lbItemList.SelectedIndex = lbItemList.Items.IndexOf(itemName);
                }
            } else if (menuItem.Name.CompareTo("lbItemRenameMenu") == 0 && itemName != null) {
                //  図面名の変更
                itemName = mFileData.renameItem(itemName);
                if (0 < itemName.Length) {
                    mCommandOpe.mDataFilePath = mFileData.getItemFilePath(itemName);
                    mCommandOpe.loadFile();
                    lbItemList.ItemsSource = mFileData.getItemFileList();
                    lbItemList.SelectedIndex = lbItemList.Items.IndexOf(itemName);
                }
            } else if (menuItem.Name.CompareTo("lbItemRemoveMenu") == 0 && itemName != null) {
                //  図面の削除
                if (mFileData.removeItem(itemName)) {
                    lbItemList.ItemsSource = mFileData.getItemFileList();
                    mCommandOpe.mDataFilePath = "";
                    if (0 < lbItemList.Items.Count)
                        lbItemList.SelectedIndex = 0;
                }
            }
            dispTitle();
        }

        /// <summary>
        /// ツリービューの部品選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComponent_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeParts) {
                TreeParts item = e.NewValue as TreeParts;
                selectItemDisp(item.mIndex);
            }
        }

        /// <summary>
        /// ツリービューのコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComponentMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            TreeParts item = tvComponent.SelectedItem as TreeParts;
            if (item != null) {
                if (menuItem.Name.CompareTo("tvComponentRmoveMenu") == 0) {
                    removeComponent(item.mIndex);
                } else if (menuItem.Name.CompareTo("tvComponentCopyMenu") == 0) {

                } else if (menuItem.Name.CompareTo("tvComponentMoveMenu") == 0) {

                }
            }
            dispTitle();
        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="key"></param>
        /// <param name="control"></param>
        /// <param name="shift"></param>
        private void keyCommand(Key key, bool control, bool shift)
        {
            if (mDrawing.mDispMode == DISPMODE.DISP3D) {
                // 3D表示
                mDrawing.key3DMove(mModelData.mCurParts, key, control, shift);
            } else {
                //  2D表示
                mDrawing.key2DMove(mModelData.mCurParts, key, control, shift);
            }
        }

        /// <summary>
        /// Primitiveデータの作成登録
        /// </summary>
        /// <param name="ope"></param>
        /// <param name="parts"></param>
        /// <param name="locList"></param>
        private void definData(OPERATION ope, Parts parts, List<PointD> locList, bool last = false)
        {
            mCommandOpe.mDispeMode = mDrawing.mDispMode;
            if (mCommandOpe.defineData(ope, mModelData.mPickElement, locList, last)) {
                commandClear();
            }
        }

        /// <summary>
        /// コマンド処理をクリア
        /// </summary>
        /// <param name="opeMode"></param>
        private void commandClear()
        {
            mCommandOpe.mOperation = OPERATION.non;
            mOperationMode = OPEMODE.non;
            mLocList.Clear();
            mModelData.mPickElement.Clear();

            setTreeData(mModelData.mRootParts);         //  Treeに更新
            mDrawing.set3DData(mModelData.mCurParts);   //  Surfacedata再作成
            mDrawing.partsDraw(mModelData.mCurParts);   //  再表示
        }

        /// <summary>
        /// ピックリストの取得
        /// </summary>
        /// <param name="pickPos">ピック位置</param>
        /// <returns>ElementIndexリスト</returns>
        private List<int> getPickNo(PointD pickPos)
        {
            double xd = mDrawing.screen2WorldXLength(mPickBoxSize);
            Box b = new Box(pickPos, xd);
            return mModelData.findIndex(mModelData.mCurParts, b, mDrawing.mDispMode);
        }

        /// <summary>
        /// 指定IndexのPartsまたはElementを表示する
        /// </summary>
        /// <param name="index">Index</param>
        private void selectItemDisp(int index)
        {
            Parts parts = mModelData.searchIndexParts(index);
            if (parts != null) {
                mModelData.mCurParts = parts;
                if (parts.mIndex == index) {
                    mModelData.mCurElement = null;
                    mDrawing.set3DData(mModelData.mCurParts);   //  3D Surfacedata
                    mDrawing.partsDraw(mModelData.mCurParts);
                } else {
                    Element element = mModelData.searchIndexElement(parts, index);
                    mModelData.mCurElement = element;
                    if (element != null) {
                        mDrawing.set3DData(element);
                        mDrawing.partsDraw(element);
                    }
                }
                dispTitle();
            }
        }

        /// <summary>
        /// PartsまたはElementの削除
        /// </summary>
        /// <param name="index"></param>
        private void removeComponent(int index)
        {
            mModelData.removeItem(index);
            mDrawing.set3DData(mModelData.mCurParts);   //  3D Surfacedata
            mDrawing.partsDraw(parts);
            setTreeData(mModelData.mRootParts);
        }

        /// <summary>
        /// 2D表示のスクロール
        /// </summary>
        /// <param name="ps">始点(Screen)</param>
        /// <param name="pe">終点(Screen)</param>
        private void scroll(Point ps, Point pe)
        {
            mDrawing.partsScroll(mModelData.mCurParts, pe.X - ps.X, pe.Y - ps.Y);
        }

        /// <summary>
        /// 2D表示の拡大縮小
        /// </summary>
        /// <param name="wp">拡大中心座標</param>
        /// <param name="scaleStep">拡大率</param>
        private void zoom(PointD wp, double scaleStep)
        {
            mDrawing.partsZoom(mModelData.mCurParts, wp, scaleStep);
        }

        /// <summary>
        /// 操作モードとマウス位置の表示
        /// </summary>
        /// <param name="wpos"></param>
        private void dispStatus(PointD wpos)
        {
            if (mPrePosition == null)
                return;
            if (wpos == null)
                wpos = mPrePosition;
            tbStatus.Text = $"Mode [{mOperationMode}] Pick [{mModelData.mPickElement.Count}] Loc [{mLocList.Count}] {wpos.ToString("f2")}";
        }

        /// <summary>
        /// 編集中の部品名の表示
        /// </summary>
        private void dispTitle()
        {
            string filename = Path.GetFileNameWithoutExtension(mCommandOpe.mDataFilePath);
            if (mModelData.mCurParts == null)
                Title = $"Note3D [{filename}]";
            else if (mModelData.mCurElement == null)
                Title = $"Note3D [{filename}] [{mModelData.mCurParts.mName}]";
            else
                Title = $"Note3D [{filename}] [{mModelData.mCurParts.mName}][{mModelData.mCurElement.mName}]";
        }

        /// <summary>
        /// Partsデータをツリーに登録
        /// </summary>
        /// <param name="parts"></param>
        public void setTreeData(Parts parts)
        {
            mParts.Clear();
            mParts.Add(cnvParts2TreeParts(parts));
            tvComponent.ItemsSource = mParts;
        }

        /// <summary>
        /// PartsデータをTreeParts(表示用)に変換
        /// </summary>
        /// <param name="parts">Partsデータ</param>
        /// <returns>TreePartsデー</returns>
        private TreeParts cnvParts2TreeParts(Parts parts)
        {
            TreeParts treeparts = new TreeParts();
            treeparts.mName = parts.mName;
            treeparts.mIndex = parts.mIndex;
            treeparts.mParts = new List<TreeParts>();
            //  Partsの登録
            for (int i = 0; i < parts.mParts.Count; i++) {
                treeparts.mParts.Add(cnvParts2TreeParts(parts.mParts[i]));
            }
            //  Elementの登録
            for (int i = 0; i < parts.mElements.Count; i++) {
                TreeParts element = new TreeParts();
                element.mName = parts.mElements[i].mName;
                element.mIndex = parts.mElements[i].mIndex;
                treeparts.mParts.Add(element);
            }
            return treeparts;
        }
    }
}