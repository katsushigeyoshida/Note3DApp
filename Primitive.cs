using CoreLib;
using System.Windows.Media;

namespace Note3DApp
{
    /// <summary>
    /// Primitive 3D図形を表すための最小単位
    /// 3D図形の元となる2Dの座標データとそれを元にさくせいされた
    /// 3DSurfaceの3D座標データからなる
    /// 
    /// Primitive
    ///     共通
    ///     List<Point3D> createVertexList()                            3D座標データ作成
    ///     Element createElement()                                     Elementデータ(Surface)の作成
    ///     void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)      2D表示
    ///     bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)   bobでピック有無
    ///     
    ///  List<Point3D> cnvPointD2Point3D(List<PointD> plist, PRIMITIVEFACE face)    2Dデータから3Dデータに変換
    ///  bool insideCrossChk(List<PointD> plist, Box b, bool close = false)         ポリラインがBoxの内部かBoxと交差しているかを調べる
    ///  
    /// 
    /// </summary>
    public enum PrimitiveId
    {
        Non, Line, Arc, Polyline, Polygon, WireCube,
        Cube, Cylinder, Sphere, Cone,
        Rotation, Push, Sweep
    }

    /// <summary>
    /// Primitiveの属性
    /// </summary>
    public abstract class Primitive
    {
        public PrimitiveId mPrimitiveId = PrimitiveId.Non;                      //  種別
        public Brush mLineColor = Brushes.Black;                                //  線の色
        public double mLineThickness = 1.0;                                     //  線の太さ
        public List<Brush> mFaceColors = new List<Brush>() { Brushes.Black };   //  面の色
        public ELEMENTTYPE mElementType;                                        //  3D表示方法(LINES,LINE_STRIP...
        public DISPMODE mPrimitiveFace = DISPMODE.XY;                     //  表示面(xy,yz,zx)
        public List<Point3D> mVertexList;                                       //  3D座標データ

        public YLib ylib = new YLib();

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        public abstract void createVertexList();

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns>Element</returns>
        public abstract Element createElement();

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            List<List<PointD>> pplist = conv3DVertex2PolylineList(mVertexList, mp, face);
            foreach (var plist in pplist)
                draw.drawWPolyline(plist);
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        //public abstract bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face);
        public bool pickChk(Box b, double[,] matrix, DISPMODE face)
        {
            List<List<PointD>> pplist = conv3DVertex2PolylineList(mVertexList, matrix, face);
            foreach (var plist in pplist) {
                if (0 < b.intersection(plist, false, true).Count ||  b.insideChk(plist))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public abstract string[] toDataList();

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public abstract void setDataList(string[] list);

        /// <summary>
        /// プロパティデータを文字列リストに変換
        /// </summary>
        /// <returns>文字列リスト</returns>
        public string[] toPropertyList()
        {
            List<string> dataList = new List<string> {
                "PrimitiveId",      mPrimitiveId.ToString(),
                "PrimitiveFace",    mPrimitiveFace.ToString(),
                "ElelmentType",     mElementType.ToString(),
                "LineColor",        ylib.getBrushName(mLineColor),
                "LineThickness",    mLineThickness.ToString(),
                "FaceColors",       mFaceColors.Count.ToString()
            };
            for (int i = 0; i < mFaceColors.Count; i++)
                dataList.Add(ylib.getBrushName(mFaceColors[i]));

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列データを設定する
        /// </summary>
        /// <param name="list">文字列リスト</param>
        public void setPropertyList(string[] list)
        {
            if (list == null || list.Length == 0)
                return;
            int ival;
            double val;
            for (int i = 0; i < list.Length; i++) {
                if (list[i] == "PrimitiveId") {
                    mPrimitiveId = (PrimitiveId)Enum.Parse(typeof(PrimitiveId), list[++i]);
                } else if (list[i] == "PrimitiveFace") {
                    mPrimitiveFace = (DISPMODE)Enum.Parse(typeof(DISPMODE), list[++i]);
                } else if (list[i] == "ElelmentType") {
                    mElementType = (ELEMENTTYPE)Enum.Parse(typeof(ELEMENTTYPE), list[++i]);
                } else if (list[i] == "LineColor") {
                    mLineColor = ylib.getBrsh(list[++i]);
                } else if (list[i] == "LineThickness") {
                    mLineThickness = double.TryParse(list[++i], out val) ? val : 1;
                } else if (list[i] == "FaceColors") {
                    mFaceColors.Clear();
                    int count = int.TryParse(list[++i], out ival) ? ival : 0;
                    for (int j = 0; j < count; j++) {
                        mFaceColors.Add(ylib.getBrsh(list[++i]));
                    }
                }
            }
        }

        /// <summary>
        /// 3Dの座標データをPolylineのリストに変換
        /// </summary>
        /// <param name="vertexList">3D座標データ</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>2D座標リスト</returns>
        private List<List<PointD>> conv3DVertex2PolylineList(List<Point3D> vertexList, double[,] mp, DISPMODE face)
        {
            List<List<PointD>> pplist = new List<List<PointD>>();
            List<Point3D> vlist = vertexList.ConvertAll(p => p.toMatrix(mp));
            List<PointD> plist = cnvPoint3D2PointD(vlist, face);
            List<PointD> buf = new List<PointD>();

            for (int i = 0; i < plist.Count; i++) {
                buf.Add(plist[i]);
                if (mElementType == ELEMENTTYPE.LINES) {
                    if (buf.Count == 2) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                    }
                } else if (mElementType == ELEMENTTYPE.LINE_STRIP) {
                    if (buf.Count == 2) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                        buf.Add(plist[i]);
                    }
                } else if (mElementType == ELEMENTTYPE.LINE_LOOP) {
                    if (buf.Count == 2) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                        buf.Add(plist[i]);
                        if (i == plist.Count - 1) {
                            buf.Add(plist[0]);
                            pplist.Add(buf);
                        }
                    }
                } else if (mElementType == ELEMENTTYPE.TRIANGLES) {
                    if (buf.Count == 3) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                    }
                } else if (mElementType == ELEMENTTYPE.QUADS) {
                    if (buf.Count == 4) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                    }
                } else if (mElementType == ELEMENTTYPE.TRIANGLE_STRIP) {
                    if (buf.Count == 3) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                        buf.Add(plist[i - 1]);
                        buf.Add(plist[i]);
                    }
                } else if (mElementType == ELEMENTTYPE.QUAD_STRIP) {
                    if (buf.Count == 4) {
                        PointD tp = buf[3];
                        buf[3] = buf[2];
                        buf[2] = tp;
                        pplist.Add(buf);
                        buf = new List<PointD>();
                        buf.Add(plist[i - 1]);
                        buf.Add(plist[i]);
                    }
                } else if (mElementType == ELEMENTTYPE.TRIANGLE_FAN) {
                    if (buf.Count == 3) {
                        pplist.Add(buf);
                        buf = new List<PointD>();
                        buf.Add(plist[0]);
                    }
                }
            }
            if (mElementType == ELEMENTTYPE.POLYGON) {
                pplist.Add(buf);
            }
            return pplist;
        }

        /// <summary>
        /// 2Dデータから3Dデータに変換
        /// </summary>
        /// <param name="plist">2D座標リスト</param>
        /// <param name="face">座標面(xy/yz/zx)</param>
        /// <returns></returns>
        public List<Point3D> cnvPointD2Point3D(List<PointD> plist, DISPMODE face)
        {
            List<Point3D> vertexList;
            if (face == DISPMODE.XY)
                vertexList = plist.ConvertAll(p => new Point3D(p.x, p.y, 0));
            else if (face == DISPMODE.YZ)
                vertexList = plist.ConvertAll(p => new Point3D(0, p.x, p.y));
            else
                vertexList = plist.ConvertAll(p => new Point3D(p.y, 0, p.x));
            return vertexList;
        }

        /// <summary>
        /// 3Dデータから2Dデータに変換
        /// </summary>
        /// <param name="plist">3D座標リスト</param>
        /// <param name="face">座標面</param>
        /// <returns>2D座標リスト</returns>
        public List<PointD> cnvPoint3D2PointD(List<Point3D> plist, DISPMODE face)
        {
            List<PointD> vertexList;
            if (face == DISPMODE.XY)
                vertexList = plist.ConvertAll(p => p.toPointXY());
            else if (face == DISPMODE.YZ)
                vertexList = plist.ConvertAll(p => p.toPointYZ());
            else
                vertexList = plist.ConvertAll(p => p.toPointZX());
            return vertexList;
        }

        /// <summary>
        /// ポリラインがBoxの内部かBoxと交差しているかを調べる
        /// </summary>
        /// <param name="plist">2D座標リスト</param>
        /// <param name="b">Box</param>
        /// <returns>判定結果</returns>
        public bool insideCrossChk(List<PointD> plist, Box b, bool close = false)
        {
            if (close)
                plist.Add(plist[0]);
            if (b.insideChk(plist))
                return true;
            if (0 < b.intersection(plist).Count)
                return true;
            return false;
        }
    }

    /// <summary>
    /// 線分
    /// </summary>
    public class LinePrimitive : Primitive
    {
        public PointD mSp;
        public PointD mEp;

        public LinePrimitive()
        {
            mSp = new PointD();
            mEp = new PointD();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        /// <param name="face">作成面</param>
        public LinePrimitive(PointD sp, PointD ep, DISPMODE face = DISPMODE.XY)
        {
            mPrimitiveId = PrimitiveId.Line;
            mElementType = ELEMENTTYPE.LINES;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns>座標リスト</returns>
        public override void createVertexList()
        {
            mVertexList = new List<Point3D>() {
                new Point3D(mSp, (int)mPrimitiveFace),
                new Point3D(mEp, (int)mPrimitiveFace),
            };
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns>Element</returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElementType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public override string[] toDataList()
        {
            List<string> dataList = new List<string>() {
                "LineData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
            };

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public override void setDataList(string[] list)
        {
            if (0 == list.Length || list[0] != "LineData")
                return;
            double val;
            for (int i = 1; i < list.Length; i++) {
                if (list[i] == "Sp") {
                    mSp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mSp.y = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "Ep") {
                    mEp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mEp.y = double.TryParse(list[++i], out val) ? val : 0;
                }
            }
        }
    }

    /// <summary>
    /// 円弧プリミティブ
    /// </summary>
    public class ArcPrimitive : Primitive
    {
        public ArcD mArc;
        public int mDiv = 40;

        public ArcPrimitive()
        {
            mArc = new ArcD();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="arc">円弧データ</param>
        /// <param name="face">作成面</param>
        public ArcPrimitive(ArcD arc, DISPMODE face = DISPMODE.XY)
        {
            mPrimitiveId = PrimitiveId.Arc;
            mElementType = ELEMENTTYPE.LINE_STRIP;
            mPrimitiveFace = face;
            mArc = arc;
            createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        public override void createVertexList()
        {
            double da = (mArc.mEa - mArc.mSa) / mDiv;
            List<PointD> plist = mArc.toPointList(mDiv);
            mVertexList = cnvPointD2Point3D(plist, mPrimitiveFace);
        }

        /// <summary>
        /// Elementデータの作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElementType, mFaceColors);
            return element;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public override string[] toDataList()
        {
            List<string> dataList = new List<string>() {
                "ArcData",
                "Arc", mArc.mCp.x.ToString(), mArc.mCp.y.ToString(), mArc.mR.ToString(),
                mArc.mSa.ToString(), mArc.mEa.ToString(),
                "Div", mDiv.ToString()
            };

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public override void setDataList(string[] list)
        {
            if (0 == list.Length || list[0] != "ArcData")
                return;
            int ival;
            double val;
            for (int i = 1; i < list.Length; i++) {
                if (list[i] == "Arc") {
                    mArc.mCp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mArc.mCp.y = double.TryParse(list[++i], out val) ? val : 0;
                    mArc.mR = double.TryParse(list[++i], out val) ? val : 0;
                    mArc.mSa = double.TryParse(list[++i], out val) ? val : 0;
                    mArc.mEa = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "Div") {
                    mDiv = int.TryParse(list[++i], out ival) ? ival : 0;
                }
            }
        }
    }

    /// <summary>
    /// ポリゴンプリミティブ
    /// </summary>
    public class PolygonPrimitive : Primitive
    {
        public List<PointD> mPolygon;

        public PolygonPrimitive()
        {
            mPolygon = new List<PointD>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="points">座標リスト</param>
        /// <param name="face">作成面</param>
        public PolygonPrimitive(List<PointD> points, DISPMODE face = DISPMODE.XY)
        {
            mPrimitiveId = PrimitiveId.Polygon;
            mElementType = ELEMENTTYPE.LINE_LOOP;
            mPrimitiveFace = face;
            mPolygon = points.ConvertAll(p => p.toCopy());
            createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        public override void createVertexList()
        {
            mVertexList = cnvPointD2Point3D(mPolygon, mPrimitiveFace);
        }

        /// <summary>
        /// Elementデータの作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElementType, mFaceColors);
            return element;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public override string[] toDataList()
        {
            List<string> dataList = new List<string>() {
                "PolygonData", "Size", mPolygon.Count.ToString()
            };
            for (int i = 0; i < mPolygon.Count; i++) {
                dataList.Add(mPolygon[i].x.ToString());
                dataList.Add(mPolygon[i].y.ToString());
            }

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public override void setDataList(string[] list)
        {
            if (0 == list.Length || list[0] != "PolygonData")
                return;
            int ival;
            double val;
            int count;
            for (int i = 1; i < list.Length; i++) {
                if (list[i] == "Size") {
                    count = int.TryParse(list[++i], out ival) ? ival : 0;
                } else {
                    PointD p = new PointD();
                    p.x = double.TryParse(list[i], out val) ? val : 0;
                    p.y = double.TryParse(list[++i], out val) ? val : 0;
                    mPolygon.Add(p);
                }
            }
        }
    }

    /// <summary>
    /// ワイヤーキューブ
    /// </summary>
    public class WireCubePrimitive : Primitive
    {
        public PointD mSp;      //  対角点(始点)
        public PointD mEp;      //  対角点(終点)
        public double mH;       //  高さ


        public WireCubePrimitive()
        {
            mSp = new PointD();
            mEp = new PointD();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        /// <param name="face">作成面</param>
        public WireCubePrimitive(PointD sp, PointD ep, double h = 1, DISPMODE face = DISPMODE.XY)
        {
            mPrimitiveId = PrimitiveId.WireCube;
            mElementType = ELEMENTTYPE.LINES;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            mH = h;
           createVertexList();

        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns></returns>
        public override void createVertexList()
        {
            List<Point3D> vertexList = new List<Point3D> {
                new Point3D(mSp.x, mSp.y, -mH/2), new Point3D(mSp.x, mEp.y, -mH/2),
                new Point3D(mSp.x, mEp.y, -mH/2), new Point3D(mEp.x, mEp.y, -mH/2),
                new Point3D(mEp.x, mEp.y, -mH/2), new Point3D(mEp.x, mSp.y, -mH/2),
                new Point3D(mEp.x, mSp.y, -mH/2), new Point3D(mSp.x, mSp.y, -mH/2),
                new Point3D(mSp.x, mSp.y, -mH/2), new Point3D(mSp.x, mSp.y,  mH/2),
                new Point3D(mSp.x, mEp.y, -mH/2), new Point3D(mSp.x, mEp.y,  mH/2),
                new Point3D(mEp.x, mEp.y, -mH/2), new Point3D(mEp.x, mEp.y,  mH/2),
                new Point3D(mEp.x, mSp.y, -mH/2), new Point3D(mEp.x, mSp.y,  mH/2),
                new Point3D(mSp.x, mSp.y,  mH/2), new Point3D(mSp.x, mEp.y,  mH/2),
                new Point3D(mSp.x, mEp.y,  mH/2), new Point3D(mEp.x, mEp.y,  mH/2),
                new Point3D(mEp.x, mEp.y,  mH/2), new Point3D(mEp.x, mSp.y,  mH/2),
                new Point3D(mEp.x, mSp.y,  mH/2), new Point3D(mSp.x, mSp.y,  mH/2),
            };
            if (mPrimitiveFace == DISPMODE.YZ) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].z, vertexList[i].x, vertexList[i].y);
                }
            } else if (mPrimitiveFace == DISPMODE.ZX) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].y, vertexList[i].z, vertexList[i].x);
                }
            }
            mVertexList = vertexList;
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElementType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public override string[] toDataList()
        {
            List<string> dataList = new List<string>() {
                "WireCubeData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
                "H", mH.ToString()
            };

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public override void setDataList(string[] list)
        {
            if (0 == list.Length || list[0] != "WireCubeData")
                return;
            double val;
            for (int i = 1; i < list.Length; i++) {
                if (list[i] == "Sp") {
                    mSp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mSp.y = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "Ep") {
                    mEp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mEp.y = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "H") {
                    mH = double.TryParse(list[++i], out val) ? val : 0;
                }
            }
        }
    }


    /// <summary>
    /// キューブ
    /// </summary>
    public class CubePrimitive : Primitive
    {
        public PointD mSp;      //  対角点(始点)
        public PointD mEp;      //  対角点(終点)
        public double mH;       //  高さ

        public CubePrimitive()
        {
            mSp = new PointD();
            mEp = new PointD();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        /// <param name="face">作成面</param>
        public CubePrimitive(PointD sp, PointD ep, double h = 1, DISPMODE face = DISPMODE.XY)
        {
            mPrimitiveId = PrimitiveId.Cube;
            mElementType = ELEMENTTYPE.QUADS;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            mH = h;
            createVertexList();

        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        public override void createVertexList()
        {
            List<Point3D> vertexList = new List<Point3D> {
                //  XY面
                new Point3D(mSp.x, mSp.y,  mH/2), new Point3D(mSp.x, mEp.y,  mH/2),
                new Point3D(mEp.x, mEp.y,  mH/2), new Point3D(mEp.x, mSp.y,  mH/2),
                new Point3D(mSp.x, mSp.y, -mH/2), new Point3D(mEp.x, mSp.y, -mH/2),
                new Point3D(mEp.x, mEp.y, -mH/2), new Point3D(mSp.x, mEp.y, -mH/2),
                //  ZX面
                new Point3D(mSp.x, mEp.y, -mH/2), new Point3D(mEp.x, mEp.y, -mH/2),
                new Point3D(mEp.x, mEp.y,  mH/2), new Point3D(mSp.x, mEp.y,  mH/2),
                new Point3D(mSp.x, mSp.y, -mH/2), new Point3D(mSp.x, mSp.y,  mH/2),
                new Point3D(mEp.x, mSp.y,  mH/2), new Point3D(mEp.x, mSp.y,  -mH/2),
                //  YZ面
                new Point3D(mEp.x, mSp.y, -mH/2), new Point3D(mEp.x, mSp.y,  mH/2),
                new Point3D(mEp.x, mEp.y,  mH/2), new Point3D(mEp.x, mEp.y, -mH/2),
                new Point3D(mSp.x, mSp.y, -mH/2), new Point3D(mSp.x, mEp.y, -mH/2),
                new Point3D(mSp.x, mEp.y,  mH/2), new Point3D(mSp.x, mSp.y,  mH/2),
            };

            if (mPrimitiveFace == DISPMODE.YZ) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].z, vertexList[i].x, vertexList[i].y);
                }
            } else if (mPrimitiveFace == DISPMODE.ZX) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].y, vertexList[i].z, vertexList[i].x);
                }
            }
            mVertexList = vertexList;
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElementType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 固有データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public override string[] toDataList()
        {
            List<string> dataList = new List<string>() {
                "CubeData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
                "H", mH.ToString()
            };

            return dataList.ToArray();
        }

        /// <summary>
        /// 文字列配列から固有データを設定
        /// </summary>
        /// <param name="list">文字列配列</param>
        public override void setDataList(string[] list)
        {
            if (0 == list.Length || list[0] != "CubeData")
                return;
            double val;
            for (int i = 1; i < list.Length; i++) {
                if (list[i] == "Sp") {
                    mSp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mSp.y = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "Ep") {
                    mEp.x = double.TryParse(list[++i], out val) ? val : 0;
                    mEp.y = double.TryParse(list[++i], out val) ? val : 0;
                } else if (list[i] == "H") {
                    mH = double.TryParse(list[++i], out val) ? val : 0;
                }
            }

        }
    }
}
