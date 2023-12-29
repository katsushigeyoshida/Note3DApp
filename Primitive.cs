using CoreLib;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

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
    public enum PRIMITIVEFACE { xy, yz, zx }

    /// <summary>
    /// Primitiveの属性
    /// </summary>
    public abstract class Primitive
    {
        public PrimitiveId mPrimitiveId = PrimitiveId.Non;
        public Brush mLineColor = Brushes.Black;
        public double mLineThickness = 1.0;
        public List<Brush> mFaceColors = new List<Brush>() { Brushes.Black };
        public ELEMENTTYPE mElelmentType;
        public PRIMITIVEFACE mPrimitiveFace = PRIMITIVEFACE.xy;
        public List<Point3D> mVertexList;

        public YLib ylib = new YLib();
        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns>座標リスト</returns>
        public abstract List<Point3D> createVertexList();

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns>Element</returns>
        public abstract Element createElement();

        /// <summary>
        /// 2D表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public abstract void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face);

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public abstract bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face);

        public abstract List<string> toDataList();

        public abstract void setDataList(List<string> list);

        /// <summary>
        /// プロパティデータを文字列リストに変換
        /// </summary>
        /// <returns>文字列リスト</returns>
        public List<string> toPropertyList()
        {
            List<string> dataList = new List<string> {
                "PrimitiveId",      mPrimitiveId.ToString(),
                "PrimitiveFace",    mPrimitiveFace.ToString(),
                "ElelmentType",     mElelmentType.ToString(),
                "LineColor",        ylib.getBrushName(mLineColor),
                "LineThickness",    mLineThickness.ToString(),
                "FaceColors",       mFaceColors.Count.ToString()
            };
            for (int i = 0; i < mFaceColors.Count; i++)
                dataList.Add(ylib.getBrushName(mFaceColors[i]));

            return dataList;
        }

        /// <summary>
        /// 文字列データを設定する
        /// </summary>
        /// <param name="list">文字列リスト</param>
        public void setPropertyList(List<string> list)
        {
            if (list == null || list.Count == 0)
                return;
            int ival;
            double val;
            for (int i = 0; i < list.Count; i++) {
                if (list[i] == "PrimitiveId") {
                    mPrimitiveId = (PrimitiveId)Enum.Parse(typeof(PrimitiveId), list[++i]);
                } else if (list[i] == "PrimitiveFace") {
                    mPrimitiveFace = (PRIMITIVEFACE)Enum.Parse(typeof(PRIMITIVEFACE), list[++i]);
                } else if (list[i] == "ElelmentType") {
                    mElelmentType = (ELEMENTTYPE)Enum.Parse(typeof(ELEMENTTYPE), list[++i]);
                } else if (list[i] == "LineColor") {
                    mLineColor = ylib.getBrsh(list[++i]);
                } else if (list[i] == "LineThickness") {
                    mLineThickness = double.TryParse(list[++i], out val) ? val : 1;
                } else if (list[i] == "FaceColors") {
                    int count = int.TryParse(list[++i], out ival) ? ival : 0;
                    for (int j = 0; j < count; j++) {
                        mFaceColors.Add(ylib.getBrsh(list[++i]));
                    }
                }
            }
        }

        /// <summary>
        /// 2Dデータから3Dデータに変換
        /// </summary>
        /// <param name="plist">2D座標リスト</param>
        /// <param name="face">座標面(xy/yz/zx)</param>
        /// <returns></returns>
        public List<Point3D> cnvPointD2Point3D(List<PointD> plist, PRIMITIVEFACE face)
        {
            List<Point3D> vertexList;
            if (mPrimitiveFace == PRIMITIVEFACE.xy)
                vertexList = plist.ConvertAll(p => new Point3D(p.x, p.y, 0));
            else if (mPrimitiveFace == PRIMITIVEFACE.yz)
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
        public List<PointD> cnvPoint3D2PointD(List<Point3D> plist, PRIMITIVEFACE face)
        {
            List<PointD> vertexList;
            if (face == PRIMITIVEFACE.xy)
                vertexList = plist.ConvertAll(p => p.toPointXY());
            else if (face == PRIMITIVEFACE.yz)
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
        public LinePrimitive(PointD sp, PointD ep, PRIMITIVEFACE face = PRIMITIVEFACE.xy)
        {
            mPrimitiveId = PrimitiveId.Line;
            mElelmentType = ELEMENTTYPE.LINES;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            mVertexList = createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns>座標リスト</returns>
        public override List<Point3D> createVertexList()
        {
            List<Point3D> vertexList = new List<Point3D>() {
                new Point3D(mSp, (int)mPrimitiveFace),
                new Point3D(mEp, (int)mPrimitiveFace),
            };
            return vertexList;
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns>Element</returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public override void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            PointD ps, pe;
            if (face == DISPMODE.disp2DXY) {
                ps = mVertexList[0].toMatrix(mp).toPointXY();
                pe = mVertexList[1].toMatrix(mp).toPointXY();
            } else if (face == DISPMODE.disp2DYZ) {
                ps = mVertexList[0].toMatrix(mp).toPointYZ();
                pe = mVertexList[1].toMatrix(mp).toPointYZ();
            } else if (face == DISPMODE.disp2DZX) {
                ps = mVertexList[0].toMatrix(mp).toPointZX();
                pe = mVertexList[1].toMatrix(mp).toPointZX();
            } else {
                return;
            }
            draw.drawWLine(ps, pe);
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public override bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)
        {
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(matrix));
            return insideCrossChk(cnvPoint3D2PointD(vlist, face), b);
        }

        public override List<string> toDataList()
        {
            List<string> dataList = new List<string>() {
                "LineData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
            };

            return dataList;
        }

        public override void setDataList(List<string> list)
        {
            if (0 == list.Count || list[0] != "LineData")
                return;
            double val;
            for (int i = 1; i < list.Count; i++) {
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="arc">円弧データ</param>
        /// <param name="face">作成面</param>
        public ArcPrimitive(ArcD arc, PRIMITIVEFACE face = PRIMITIVEFACE.xy)
        {
            mPrimitiveId = PrimitiveId.Arc;
            mElelmentType = ELEMENTTYPE.LINE_STRIP;
            mPrimitiveFace = face;
            mArc = arc;
            mVertexList = createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns></returns>
        public override List<Point3D> createVertexList()
        {
            double da = (mArc.mEa - mArc.mSa) / mDiv;
            List<PointD> plist = mArc.toPointList(mDiv);
            List<Point3D> vertexList = cnvPointD2Point3D(plist, mPrimitiveFace);
            return vertexList;
        }

        /// <summary>
        /// Elementデータの作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);
            return element;
        }

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public override void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            List<PointD> pList;
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(mp));
            if (face == DISPMODE.disp2DXY) {
                pList = vlist.ConvertAll(p => new PointD(p.x, p.y));
            } else if (face == DISPMODE.disp2DYZ) {
                pList = vlist.ConvertAll(p => new PointD(p.y, p.z));
            } else {
                pList = vlist.ConvertAll(p => new PointD(p.z, p.x));
            }
            draw.drawWPolyline(pList);
        }


        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public override bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)
        {
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(matrix));
            return insideCrossChk(cnvPoint3D2PointD(vlist, face), b);
        }

        public override List<string> toDataList()
        {
            List<string> dataList = new List<string>() {
                "ArcData",
                "Arc", mArc.mCp.x.ToString(), mArc.mCp.y.ToString(), mArc.mR.ToString(),
                mArc.mSa.ToString(), mArc.mEa.ToString(),
                "Div", mDiv.ToString()
            };

            return dataList;
        }
        public override void setDataList(List<string> list)
        {
            if (0 == list.Count || list[0] != "ArcData")
                return;
            int ival;
            double val;
            for (int i = 1; i < list.Count; i++) {
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="points">座標リスト</param>
        /// <param name="face">作成面</param>
        public PolygonPrimitive(List<PointD> points, PRIMITIVEFACE face = PRIMITIVEFACE.xy)
        {
            mPrimitiveId = PrimitiveId.Polygon;
            mElelmentType = ELEMENTTYPE.LINE_LOOP;
            mPrimitiveFace = face;
            mPolygon = points;
            mVertexList = createVertexList();
        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns></returns>
        public override List<Point3D> createVertexList()
        {
            List<Point3D> vertexList = cnvPointD2Point3D(mPolygon, mPrimitiveFace);
            return vertexList;
        }

        /// <summary>
        /// Elementデータの作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);
            return element;
        }

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public override void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            List<PointD> pList;
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(mp));
            if (face == DISPMODE.disp2DXY)
                pList = vlist.ConvertAll(p => new PointD(p.x, p.y));
            else if (face == DISPMODE.disp2DYZ)
                pList = vlist.ConvertAll(p => new PointD(p.y, p.z));
            else
                pList = vlist.ConvertAll(p => new PointD(p.z, p.x));
            draw.drawWPolygon(pList);
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public override bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)
        {
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(matrix));
            return insideCrossChk(cnvPoint3D2PointD(vlist, face), b, true);
        }

        public override List<string> toDataList()
        {
            List<string> dataList = new List<string>() {
                "PolygonData", "Size", mPolygon.Count.ToString()
            };
            for (int i = 0; i < mPolygon.Count; i++) {
                dataList.Add(mPolygon[i].x.ToString());
                dataList.Add(mPolygon[i].y.ToString());
            }

            return dataList;
        }
        public override void setDataList(List<string> list)
        {
            if (0 == list.Count || list[0] != "PolygonData")
                return;
            int ival;
            double val;
            int count;
            for (int i = 1; i < list.Count; i++) {
                if (list[i] == "Size") {
                    count = int.TryParse(list[++i], out ival) ? ival : 0;
                } else {
                    PointD p = new PointD();
                    p.x = double.TryParse(list[++i], out val) ? val : 0;
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        /// <param name="face">作成面</param>
        public WireCubePrimitive(PointD sp, PointD ep, double h = 1, PRIMITIVEFACE face = PRIMITIVEFACE.xy)
        {
            mPrimitiveId = PrimitiveId.WireCube;
            mElelmentType = ELEMENTTYPE.LINES;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            mH = h;
            mVertexList = createVertexList();

        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns></returns>
        public override List<Point3D> createVertexList()
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
            if (mPrimitiveFace == PRIMITIVEFACE.yz) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].z, vertexList[i].x, vertexList[i].y);
                }
            } else if (mPrimitiveFace == PRIMITIVEFACE.zx) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].y, vertexList[i].z, vertexList[i].x);
                }
            }
            return vertexList;
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public override void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(mp));
            for (int i = 0; i < vlist.Count - 1; i+= 2) {
                if (face == DISPMODE.disp2DXY)
                    draw.drawWLine(vlist[i].toPointXY(), vlist[i + 1].toPointXY());
                else if (face == DISPMODE.disp2DYZ)
                    draw.drawWLine(vlist[i].toPointYZ(), vlist[i + 1].toPointYZ());
                else
                    draw.drawWLine(vlist[i].toPointZX(), vlist[i + 1].toPointZX());
            }
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public override bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)
        {
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(matrix));
            List<PointD> points = cnvPoint3D2PointD(vlist, face);
            for (int i = 0; i < points.Count; i += 2) {
                List<PointD> plist = new List<PointD>() { points[i], points[i + 1] };
                if (insideCrossChk(plist, b))
                    return true;
            }
            return false;
        }

        public override List<string> toDataList()
        {
            List<string> dataList = new List<string>() {
                "WireCubeData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
                "H", mH.ToString()
            };

            return dataList;
        }
        public override void setDataList(List<string> list)
        {
            if (0 == list.Count || list[0] != "WireCubeData")
                return;
            double val;
            for (int i = 1; i < list.Count; i++) {
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
    /// キューブ
    /// </summary>
    public class CubePrimitive : Primitive
    {
        public PointD mSp;      //  対角点(始点)
        public PointD mEp;      //  対角点(終点)
        public double mH;       //  高さ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        /// <param name="face">作成面</param>
        public CubePrimitive(PointD sp, PointD ep, double h = 1, PRIMITIVEFACE face = PRIMITIVEFACE.xy)
        {
            mPrimitiveId = PrimitiveId.Cube;
            mElelmentType = ELEMENTTYPE.QUADS;
            mPrimitiveFace = face;
            mSp = sp;
            mEp = ep;
            mH = h;
            mVertexList = createVertexList();

        }

        /// <summary>
        /// 3D座標リストの作成
        /// </summary>
        /// <returns></returns>
        public override List<Point3D> createVertexList()
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

            if (mPrimitiveFace == PRIMITIVEFACE.yz) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].z, vertexList[i].x, vertexList[i].y);
                }
            } else if (mPrimitiveFace == PRIMITIVEFACE.zx) {
                for (int i = 0; i < vertexList.Count; i++) {
                    vertexList[i] = new Point3D(vertexList[i].y, vertexList[i].z, vertexList[i].x);
                }
            }
            return vertexList;
        }

        /// <summary>
        /// Elementデータ(Surface)の作成
        /// </summary>
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 2D 表示(XY/YZ/ZX)
        /// </summary>
        /// <param name="draw">グラフィック</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public override void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            draw.mThickness = mLineThickness;
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(mp));
            for (int i = 0; i < vlist.Count - 1; i += 2) {
                if (face == DISPMODE.disp2DXY)
                    draw.drawWLine(vlist[i].toPointXY(), vlist[i + 1].toPointXY());
                else if (face == DISPMODE.disp2DYZ)
                    draw.drawWLine(vlist[i].toPointYZ(), vlist[i + 1].toPointYZ());
                else
                    draw.drawWLine(vlist[i].toPointZX(), vlist[i + 1].toPointZX());
            }
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックの有無</returns>
        public override bool pickChk(Box b, double[,] matrix, PRIMITIVEFACE face)
        {
            List<Point3D> vlist = mVertexList.ConvertAll(p => p.toMatrix(matrix));
            List<PointD> points = cnvPoint3D2PointD(vlist, face);
            for (int i = 0; i < points.Count; i += 4) {
                List<PointD> plist = new List<PointD>() {
                    points[i], points[i+1], points[i+2], points[i+3], points[i]
                };
                if (insideCrossChk(plist, b))
                    return true;
            }
            return false;
        }

        public override List<string> toDataList()
        {
            List<string> dataList = new List<string>() {
                "CubeData",
                "Sp", mSp.x.ToString(), mSp.y.ToString(),
                "Ep", mEp.x.ToString(), mEp.y.ToString(),
                "H", mH.ToString()
            };

            return dataList;
        }

        public override void setDataList(List<string> list)
        {
            if (0 == list.Count || list[0] != "WireCubeData")
                return;
            double val;
            for (int i = 1; i < list.Count; i++) {
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
}
