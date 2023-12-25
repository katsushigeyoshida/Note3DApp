using CoreLib;
using System.Windows.Media;

namespace Note3DApp
{
    /// <summary>
    /// Primitive 3D図形を表すための最小単位
    /// 3D図形の元となる2Dの座標データとそれを元にさくせいされた
    /// 3DSurfaceの3D座標データからなる
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
        public List<Brush> mFaceColors = new List<Brush>() { Brushes.Black };
        public ELEMENTTYPE mElelmentType;
        public PRIMITIVEFACE mPrimitiveFace = PRIMITIVEFACE.xy;
        public List<Point3D> mVertexList;

        public abstract List<Point3D> createVertexList();
        public abstract Element createElement();
        public abstract void draw2D(Y3DDraw draw, DISPMODE face);
        public abstract bool pickChk(Box b, PRIMITIVEFACE face);

        /// <summary>
        /// 2Dデータから3Dデータに返還
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
        /// <returns></returns>
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
        /// <returns></returns>
        public override Element createElement()
        {
            Element element = new Element();
            element.addVertex(mVertexList, mElelmentType, mFaceColors);

            return element;
        }

        /// <summary>
        /// 2D 表示
        /// </summary>
        /// <param name="draw"></param>
        public override void draw2D(Y3DDraw draw, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            if (face == DISPMODE.disp2DXY)
                draw.drawWLine(mVertexList[0].toPointXY(), mVertexList[1].toPointXY());
            else if (face == DISPMODE.disp2DYZ)
                draw.drawWLine(mVertexList[0].toPointYZ(), mVertexList[1].toPointYZ());
            else if (face == DISPMODE.disp2DZX)
                draw.drawWLine(mVertexList[0].toPointZX(), mVertexList[1].toPointZX());
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public override bool pickChk(Box b, PRIMITIVEFACE face)
        {
            return insideCrossChk(cnvPoint3D2PointD(mVertexList, face), b);
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
        /// 2D 表示
        /// </summary>
        /// <param name="draw"></param>
        public override void draw2D(Y3DDraw draw, DISPMODE face)
        {
            List<PointD> vertexList;
            draw.mBrush = mLineColor;
            if (face == DISPMODE.disp2DXY)
                vertexList = mVertexList.ConvertAll(p => new PointD(p.x, p.y));
            else if (face == DISPMODE.disp2DYZ)
                vertexList = mVertexList.ConvertAll(p => new PointD(p.y, p.z));
            else
                vertexList = mVertexList.ConvertAll(p => new PointD(p.z, p.x));
            draw.drawWPolyline(vertexList);
        }


        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public override bool pickChk(Box b, PRIMITIVEFACE face)
        {
            return insideCrossChk(cnvPoint3D2PointD(mVertexList, face), b);
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
        /// 2D XY表示
        /// </summary>
        /// <param name="draw"></param>
        public override void draw2D(Y3DDraw draw, DISPMODE face)
        {
            List<PointD> vertexList;
            draw.mBrush = mLineColor;
            if (face == DISPMODE.disp2DXY)
                vertexList = mVertexList.ConvertAll(p => new PointD(p.x, p.y));
            else if (face == DISPMODE.disp2DYZ)
                vertexList = mVertexList.ConvertAll(p => new PointD(p.y, p.z));
            else
                vertexList = mVertexList.ConvertAll(p => new PointD(p.z, p.x));
            draw.drawWPolygon(vertexList);
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public override bool pickChk(Box b, PRIMITIVEFACE face)
        {
            return insideCrossChk(cnvPoint3D2PointD(mVertexList, face), b, true);
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
        /// 2D XY 表示
        /// </summary>
        /// <param name="draw"></param>
        public override void draw2D(Y3DDraw draw, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            for (int i = 0; i < mVertexList.Count - 1; i+= 2) {
                if (face == DISPMODE.disp2DXY)
                    draw.drawWLine(mVertexList[i].toPointXY(), mVertexList[i + 1].toPointXY());
                else if (face == DISPMODE.disp2DYZ)
                    draw.drawWLine(mVertexList[i].toPointYZ(), mVertexList[i + 1].toPointYZ());
                else
                    draw.drawWLine(mVertexList[i].toPointZX(), mVertexList[i + 1].toPointZX());
            }
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public override bool pickChk(Box b, PRIMITIVEFACE face)
        {
            List<PointD> points = cnvPoint3D2PointD(mVertexList, face);
            for (int i = 0; i < points.Count; i += 2) {
                List<PointD> plist = new List<PointD>() { points[i], points[i + 1] };
                if (insideCrossChk(plist, b))
                    return true;
            }
            return false;
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
        /// 2D XY 表示
        /// </summary>
        /// <param name="draw"></param>
        public override void draw2D(Y3DDraw draw, DISPMODE face)
        {
            draw.mBrush = mLineColor;
            for (int i = 0; i < mVertexList.Count - 1; i += 2) {
                if (face == DISPMODE.disp2DXY)
                    draw.drawWLine(mVertexList[i].toPointXY(), mVertexList[i + 1].toPointXY());
                else if (face == DISPMODE.disp2DYZ)
                    draw.drawWLine(mVertexList[i].toPointYZ(), mVertexList[i + 1].toPointYZ());
                else
                    draw.drawWLine(mVertexList[i].toPointZX(), mVertexList[i + 1].toPointZX());
            }
        }

        /// <summary>
        /// Boxのピックの有無を調べる
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public override bool pickChk(Box b, PRIMITIVEFACE face)
        {
            List<PointD> points = cnvPoint3D2PointD(mVertexList, face);
            for (int i = 0; i < points.Count; i += 4) {
                List<PointD> plist = new List<PointD>() {
                    points[i], points[i+1], points[i+2], points[i+3], points[i]
                };
                if (insideCrossChk(plist, b))
                    return true;
            }
            return false;
        }
    }
}
