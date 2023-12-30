using CoreLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Note3DApp
{
    /// <summary>
    /// 
    /// </summary>
    public enum DISPMODE { XY, YZ, ZX, DISP3D }

    public class DataDrawing
    {
        private double mWorldSize = 2.0;                        //  3D 空間サイズ
        private Brush mBaseBackColor = null;                    //  背景色
        private BitmapSource mBitmapSource;                     //  CanvasのBitmap一時保存

        public Brush mPickColor = Brushes.Red;                  //  ピック時のカラー
        public int mScrollSize = 19;                            //  キーによるスクロール単位
        public double mGridSize = 1.0;                          //  グリッドサイズ
        public int mGridMinmumSize = 8;                         //  グリッドの最小スクリーンサイズ
        public DISPMODE mDispMode = DISPMODE.XY;                //  表示モード(XY,YZ,ZX,3D)
        public Y3DDraw mGDraw;                                  //  2D/3D表示ライブラリ

        private MainWindow mMainWindow;
        private Canvas mCanvas;
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mainWindow"></param>
        public DataDrawing(Canvas canvas, MainWindow mainWindow)
        {
            mCanvas = canvas;
            mMainWindow = mainWindow;
            mWorldSize = 30.0;
            mGDraw = new Y3DDraw(mCanvas, mCanvas.ActualWidth, mCanvas.ActualHeight);
            mGDraw.set3DWorldWindow(new Size(mCanvas.ActualWidth, mCanvas.ActualHeight), mWorldSize, mWorldSize * 5);
            mGDraw.clear();
        }

        /// <summary>
        /// Parts単位での表示データの登録
        /// </summary>
        /// <param name="parts">部品データ</param>
        public void set3DData(Parts parts)
        {
            if (parts != null) {
                mGDraw.clearData();
                mGDraw.addData(parts.cnvDrawData(ylib.unitMatrix(4)));
            }
        }

        /// <summary>
        /// Element単位で表示データを登録
        /// </summary>
        /// <param name="element">Element</param>
        public void set3DData(Element element)
        {
            mGDraw.clearData();
            mGDraw.addData(element.cnvDrawData(ylib.unitMatrix(4)));
        }

        /// <summary>
        /// 2Dでのドラッギング処理
        /// </summary>
        /// <param name="ope">操作の種別</param>
        /// <param name="locList">ロケイト座標リスト</param>
        /// <param name="lastPoint">最終マウス位置</param>
        public void dragging(OPERATION ope, List<PickData> pickData, List<PointD> locList, PointD lastPoint)
        {
            if (ope == OPERATION.non)
                return;
            mGDraw.mBrush = Brushes.Green;
            mGDraw.mFillColor = null;
            mGDraw.mLineType = 0;
            mGDraw.mThickness = 1;

            mGDraw.clear();
            mMainWindow.imScreen.Source = mBitmapSource;
            mCanvas.Children.Add(mMainWindow.imScreen);

            switch (ope) {
                case OPERATION.createLine:
                    if (locList.Count == 1)
                        mGDraw.drawWLine(locList[0], lastPoint);
                    break;
                case OPERATION.createArc:
                    if (locList.Count == 2)
                        mGDraw.drawWArc(new ArcD(locList[0], lastPoint, locList[1]), false);
                    else if (locList.Count == 1)
                        mGDraw.drawWLine(locList[0], lastPoint);
                    break;
                case OPERATION.createCircle:
                    if (0 < locList.Count)
                        mGDraw.drawWArc(new ArcD(locList[0], lastPoint.length(locList[0])));
                    break;
                case OPERATION.createRect:
                    if (locList.Count == 1)
                        mGDraw.drawWRectangle(locList[0], lastPoint);
                    break;
                case OPERATION.createPolygon:
                    if (0 < locList.Count) {
                        List<PointD> plist = locList.ConvertAll(p => p);
                        plist.Add(lastPoint);
                        mGDraw.drawWPolygon(plist);
                    }
                    break;
                case OPERATION.createWireCube:
                    if (locList.Count == 1)
                        mGDraw.drawWRectangle(locList[0], lastPoint);
                    break;
                case OPERATION.createCube:
                    if (locList.Count == 1)
                        mGDraw.drawWRectangle(locList[0], lastPoint);
                    break;
                case OPERATION.translate:
                    translateDragging(pickData, locList, lastPoint);
                    break;
                case OPERATION.rotate:
                    rotateDragging(pickData, locList, lastPoint);
                    break;
            }

            mGDraw.mPointType = 2;
            mGDraw.mPointSize = 2;
            mGDraw.drawWPoint(lastPoint);
        }

        /// <summary>
        /// 移動ドラッギング表示
        /// </summary>
        /// <param name="pickData">ピックデータ</param>
        /// <param name="locList">ロケイトリスト</param>
        /// <param name="lastPoint">最終ロケイト点</param>
        public void translateDragging(List<PickData> pickData, List<PointD> locList, PointD lastPoint)
        {
            if (0 < locList.Count) {
                PointD v = lastPoint - locList[0];
                double[,] transMatrix = translate2Dmatrix3D(v, mDispMode);
                for (int i = 0; i < pickData.Count; i++) {
                    Element ele = pickData[i].mElement.toCopy();
                    var matrix = ylib.matrixMulti(ele.mMatrix, transMatrix);
                    ele.mPrimitive.draw2D(mGDraw, matrix, mDispMode);
                }
            }
        }

        /// <summary>
        /// 回転ののドラッギング表示
        /// </summary>
        /// <param name="pickData">ピックデータ</param>
        /// <param name="locList">ロケイトリスト</param>
        /// <param name="lastPoint">最終ロケイト点</param>
        public void rotateDragging(List<PickData> pickData, List<PointD> locList, PointD lastPoint)
        {
            if (0 < locList.Count) {
                double th = locList[0].angle() - lastPoint.angle();
                double[,] transMatrix = rotate2Dmatrix3D(th, mDispMode);
                for (int i = 0; i < pickData.Count; i++) {
                    Element ele = pickData[i].mElement.toCopy();
                    var matrix = ylib.matrixMulti(ele.mMatrix, transMatrix);
                    ele.mPrimitive.draw2D(mGDraw, matrix, mDispMode);
                }
            }
        }

        /// <summary>
        /// 2Dの移動量を3Dマトリックスに変換
        /// </summary>
        /// <param name="v">2D移動ベクトル</param>
        /// <param name="dispMode">2D面</param>
        /// <returns>3D変換マトリックス</returns>
        public double[,] translate2Dmatrix3D(PointD v, DISPMODE dispMode)
        {
            if (dispMode == DISPMODE.YZ)
                return ylib.translate3DMatrix(0, v.x, v.y);
            else if (dispMode == DISPMODE.ZX)
                return ylib.translate3DMatrix(v.y, 0, v.x);
            else
                return ylib.translate3DMatrix(v.x, v.y, 0);
        }

        /// <summary>
        /// 2Dの回転角を3Dマトリックスに変換
        /// </summary>
        /// <param name="th">回転角</param>
        /// <param name="dispMode">2D面</param>
        /// <returns>3D変換マトリックス</returns>
        public double[,] rotate2Dmatrix3D(double th, DISPMODE dispMode)
        {
            if (dispMode == DISPMODE.YZ)
                return ylib.rotateX3DMatrix(th);
            else if (dispMode == DISPMODE.ZX)
                return ylib.rotateY3DMatrix(th);
            else
                return ylib.rotateZ3DMatrix(th);
        }

        /// <summary>
        /// 指定Partsの表示
        /// </summary>
        /// <param name="parts">Partsデータ</param>
        /// <param name="grid">グリッドの有無</param>
        /// <param name="bitmap">Bitmapのコピー</param>
        public void partsDraw(Parts parts, bool grid = true, bool bitmap = true)
        {
            dispInit();
            if (parts == null) return;
            if (mDispMode == DISPMODE.DISP3D) {
                //  3Dデータの表示
                mGDraw.drawSurfaceList();
            } else {
                if (grid)
                    dispGrid(mGridSize);
                partsDraw2D(parts, bitmap);
            }
        }

        /// <summary>
        /// 指定Elementの表示
        /// </summary>
        /// <param name="element">Elelemntデータ</param>
        /// <param name="grid">グリッドの有無</param>
        /// <param name="bitmap">Bitmapのコピー</param>
        public void partsDraw(Element element, bool grid = true, bool bitmap = true)
        {
            dispInit();
            if (element == null) return;
            if (mDispMode == DISPMODE.DISP3D) {
                //  3Dデータの表示
                mGDraw.drawSurfaceList();
            } else {
                if (grid)
                    dispGrid(mGridSize);
                elementDraw2D(element, bitmap);
            }
        }

        /// <summary>
        /// 2Dデータの表示
        /// </summary>
        /// <param name="parts">Partsデータ</param>
        /// <param name="bitmap">Bitmapのコピー</param>
        public void partsDraw2D(Parts parts, bool bitmap = true)
        {
            parts.draw2D(mGDraw, ylib.unitMatrix(4), mDispMode);
            if (bitmap)
                mBitmapSource = ylib.canvas2Bitmap(mCanvas);
        }

        /// <summary>
        /// 2Dデータの表示
        /// </summary>
        /// <param name="element">Elementデー</param>
        /// <param name="bitmap"Bitmapのコピー></param>
        public void elementDraw2D(Element element, bool bitmap = true)
        {
            element.mPrimitive.draw2D(mGDraw, element.mMatrix, mDispMode);
            if (bitmap)
                mBitmapSource = ylib.canvas2Bitmap(mCanvas);
        }

        /// <summary>
        /// ピック色でElement表示
        /// </summary>
        /// <param name="element"></param>
        /// <param name="bitmap"></param>
        public void pickPartDraw2D(Element element, bool bitmap = true)
        {
            Brush tmpBrush = element.mPrimitive.mLineColor;
            element.mPrimitive.mLineColor = mPickColor;
            elementDraw2D(element, bitmap);
            element.mPrimitive.mLineColor = tmpBrush;
        }

        /// <summary>
        /// 2D表示の上下左右スクロール
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void partsScroll(Parts parts, double dx, double dy)
        {
            PointD v = new PointD(mGDraw.screen2worldXlength(dx), mGDraw.screen2worldYlength(dy));
            mGDraw.mWorld.offset(v.inverse());
            //  全体再表示
            //mGDraw.mClipBox = mGDraw.mWorld;
            //partsDraw(parts, false, false);

            //  ポリゴンの塗潰しで境界線削除ためオフセットを設定
            double offset = mGDraw.screen2worldXlength(2);

            dispInit();
            mGDraw.mClipBox = mGDraw.mWorld.toCopy();

            //  横空白部分を描画
            if (0 > dx) {
                mGDraw.mClipBox.Left = mGDraw.mWorld.Right + v.x - offset;
                mGDraw.mClipBox.Width = -v.x + offset;
            } else if (0 < dx) {
                mGDraw.mClipBox.Width = v.x + offset;
            }
            if (dx != 0) {
                dispGrid(mGridSize);
                partsDraw2D(parts, false);
            }

            //  縦空白部分を描画
            mGDraw.mClipBox = mGDraw.mWorld.toCopy();
            if (0 > dy) {
                mGDraw.mClipBox.Top -= mGDraw.mWorld.Height - v.y - offset;
                mGDraw.mClipBox.Height = v.y + offset;
            } else if (0 < dy) {
                mGDraw.mClipBox.Height = -v.y + offset;
            }
            if (dy != 0) {
                dispGrid(mGridSize);
                partsDraw2D(parts, false);
            }

            //  移動した位置にBitmapの貼付け(ポリゴン塗潰しの境界線削除でoffsetを設定)
            ylib.moveImage(mCanvas, mBitmapSource, dx, dy, 1);

            //  Windowの設定を元に戻す
            mGDraw.mClipBox = mGDraw.mWorld.toCopy();
            mBitmapSource = ylib.canvas2Bitmap(mCanvas);

            //  コピーしたイメージを貼り付けなおすことで文字のクリッピングする
            //mGDraw.clear();
            //moveImage(mCanvas, mBitmapSource, 0, 0);
        }

        /// <summary>
        /// 2D表示の拡大縮小
        /// </summary>
        /// <param name="parts">3Dモデル</param>
        /// <param name="wp">拡大縮小の中心座標(World)</param>
        /// <param name="scaleStep">拡大率</param>
        public void partsZoom(Parts parts, PointD wp, double scaleStep)
        {
            mGDraw.setWorldZoom(wp, scaleStep, true);
            mGDraw.mClipBox = mGDraw.mWorld;
            partsDraw(parts);
        }

        /// <summary>
        /// マウスホィールによる3Dの拡大縮小表示
        /// </summary>
        /// <param name="parts">3Dモデル</param>
        /// <param name="scale">ホィールステップ</param>
        public void mouse3DScale(Parts parts, double scale)
        {
            if (0 != scale) {
                double scaleStep = scale > 0 ? 1.1 : 1 / 1.1;
                mGDraw.setScale3DMatrix(scaleStep, scaleStep, scaleStep);
                partsDraw(parts);
            }
        }

        /// <summary>
        /// マウスによる3D回転開始
        /// </summary>
        /// <param name="pos">スクリーン座標</param>
        public void mouse3DRotateStart(Point pos)
        {
            mGDraw.mouseMoveStart(true, pos);
        }

        /// <summary>
        /// マウスによる3D移動開始
        /// </summary>
        /// <param name="pos">スクリーン座標</param>
        public void mouse3DTlansLateStart(Point pos)
        {
            mGDraw.mouseMoveStart(false, pos);
        }

        /// <summary>
        /// マウスによる3D回転移動停止
        /// </summary>
        public void mouse3DEnd()
        {
            mGDraw.mouseMoveEnd();
        }

        /// <summary>
        /// マウスによる3D回転移動
        /// </summary>
        /// <param name="parts">3Dモデル</param>
        /// <param name="pos">座標</param>
        public void mouse3DMove(Parts parts, Point pos)
        {
            if (mGDraw.mouseMove(pos))
                partsDraw(parts);
        }

        /// <summary>
        /// キー操作による3D移動回転拡大縮小
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="key"></param>
        /// <param name="control"></param>
        /// <param name="shift"></param>
        public void key3DMove(Parts parts, System.Windows.Input.Key key, bool control, bool shift)
        {
            if(key == Key.Home && !control) {
                //  初期状態
                set3DData(parts);
                partsDraw(parts);
            } else if (mGDraw.keyMove(key, control))
                partsDraw(parts);
        }

        /// <summary>
        /// キー操作による2D表示処理
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="key"></param>
        /// <param name="control"></param>
        /// <param name="shift"></param>
        public void key2DMove(Parts parts, System.Windows.Input.Key key, bool control, bool shift)
        {
            if (control) {
                switch (key) {
                    case Key.Left: partsScroll(parts, mScrollSize, 0); break;
                    case Key.Right: partsScroll(parts, -mScrollSize, 0); break;
                    case Key.Up: partsScroll(parts, 0, mScrollSize); break;
                    case Key.Down: partsScroll(parts, 0, -mScrollSize); break;
                    case Key.PageUp: partsZoom(parts, mGDraw.mWorld.getCenter(), 1.1); break;
                    case Key.PageDown: partsZoom(parts, mGDraw.mWorld.getCenter(), 1 / 1.1); break;
                    default:
                        break;
                }
            } else if (shift) {
                switch (key) {
                    case Key.F1: break;
                    default: break;
                }
            } else {
                switch (key) {
                    case Key.F1: partsDraw2D(parts); break;              //  再表示
                    case Key.F3: dispFit(parts); break;                      //  全体表示
                    case Key.F4: partsZoom(parts, mGDraw.mWorld.getCenter(), 1.2); break;                      //  拡大表示
                    case Key.F5: partsZoom(parts, mGDraw.mWorld.getCenter(), 1 / 1.2); break;                  //  縮小表示
                    case Key.F6: dispWidthFit(parts); break;                 //  全幅表示
                    default: break;
                }
            }
        }

        public void dispFit(Parts parts)
        {

        }

        public void dispWidthFit(Parts parts)
        {

        }

        /// <summary>
        /// 画面クリア
        /// </summary>
        public void dispInit(bool bitmap = false)
        {
            mGDraw.clear();
            mGDraw.mFillColor = mBaseBackColor;
            mGDraw.mBrush = null;
            if (mGDraw.mFillColor != null)
                mGDraw.drawRectangle(mGDraw.mView);

            mGDraw.mFillColor = null;
            mGDraw.mBrush = Brushes.Black;
            if (bitmap)
                mBitmapSource = ylib.canvas2Bitmap(mCanvas);
        }

        /// <summary>
        /// 表示の初期化
        /// </summary>
        public void drawWorldFrame()
        {
            //  背景色と枠の表示
            mGDraw.clear();
            mGDraw.mFillColor = mBaseBackColor;
            mGDraw.mBrush = Brushes.Black;

            mGDraw.setViewSize(new Size(mCanvas.ActualWidth, mCanvas.ActualHeight));
            Box world = mGDraw.mWorld.toCopy();
            world.scale(world.getCenter(), 0.99);
            Rect rect = new Rect(world.TopLeft.toPoint(), world.BottomRight.toPoint());
            mGDraw.drawWRectangle(rect);
        }

        /// <summary>
        /// グリッドの表示
        /// グリッド10個おきに大玉を表示
        /// </summary>
        /// <param name="size">グリッドの間隔</param>
        public void dispGrid(double size)
        {
            if (0 < size && size < 1000) {
                mGDraw.mBrush = mGDraw.getColor("Black");
                mGDraw.mThickness = 1.0;
                mGDraw.mPointType = 0;
                while (mGridMinmumSize > mGDraw.world2screenXlength(size) && size < 1000) {
                    size *= 10;
                }
                if (mGridMinmumSize <= mGDraw.world2screenXlength(size)) {
                    //  グリッド間隔(mGridMinmumSize)dot以上を表示
                    double y = mGDraw.mWorld.Bottom - size;
                    y = Math.Floor(y / size) * size;
                    while (y < mGDraw.mWorld.Top) {
                        double x = mGDraw.mWorld.Left;
                        x = Math.Floor(x / size) * size;
                        while (x < mGDraw.mWorld.Right) {
                            PointD p = new PointD(x, y);
                            if (x % (size * 10) == 0 && y % (size * 10) == 0) {
                                //  10個おきの点
                                mGDraw.mPointSize = 2;
                                mGDraw.drawWPoint(p);
                            } else {
                                mGDraw.mPointSize = 1;
                                mGDraw.drawWPoint(p);
                            }
                            x += size;
                        }
                        y += size;
                    }
                }
            }
            //  原点(0,0)表示
            mGDraw.mBrush = mGDraw.getColor("Red");
            mGDraw.mPointType = 2;
            mGDraw.mPointSize = 5;
            mGDraw.drawWPoint(new PointD(0, 0));
        }

        /// <summary>
        /// スクリーン座標からワールド座標に変換
        /// </summary>
        /// <param name="p">スクリーン座標</param>
        /// <returns>ワールド座標</returns>
        public PointD screen2World(Point p)
        {
            return mGDraw.cnvScreen2World(new PointD(p));
        }

        /// <summary>
        /// スクリーン座標のX方向長さをワールド座標の長さに変換
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public double screen2WorldXLength(double l)
        {
            return mGDraw.screen2worldXlength(l);
        }
    }
}
