using CoreLib;
using System.Windows.Media;

namespace Note3DApp
{
    /// <summary>
    /// Element    エレメントクラス サーフェスデータの集合クラス
    ///     Element()
    ///     void clear()                    全データクリア
    ///     void matrixClear()              マトリックス(配置と姿勢)クリア
    ///     void addMatrix(double[,] mp)    マトリックスの追加
    ///     void addTranslate(Point3D v)    配置をマトリックスに追加
    ///     void addRotateX(double th)      X軸回転をマトリックスに追加
    ///     void addRotateY(double th)      Y軸回転をマトリックスに追加
    ///     void addRotateZ(double th)      Z軸回転をマトリックスに追加
    ///     void addScale(Point3D v)        拡大・縮小をマトリックスに追加
    ///     void addVertex(List<Point3D> ps, ELEMENTTYPE elementType, List<Brush> colors)   各エレメントデータをLineまたはPolygonにして座標データを登録
    ///     void setDrawData(Y3DDraw y3Ddraw, double[,] addMatrix)  要素データ(サーフェス)をY3DDrawに登録
    ///     List<Surface> cnvDrawData(double[,] addMatrix)  要素データをSurfaceデータに変換
    /// </summary>
    
    public enum ELEMENTTYPE
    {
        LINES, LINE_STRIP, LINE_LOOP,
        TRIANGLES, QUADS, POLYGON, TRIANGLE_STRIP,
        QUAD_STRIP, TRIANGLE_FAN, PARTS
    };

    /// <summary>
    /// エレメントクラス
    /// サーフェスデータの集合クラス
    /// </summary>
    public class Element
    {
        public Primitive mPrimitive;            //  プリミティブリスト
        public List<Surface> mSurfaceList;      //  サーフェスデータリスト(LINE/TRYANGLE)
        public double[,] mMatrix;               //  配置と姿勢設定マトリックス
        public string mName;                    //  エレメント名称
        public int mIndex = -1;                 //  インデックス
        public Parts? mParent = null;           //  親パーツ

        private YLib ylib = new YLib();

        public Element()
        {
            clear();
        }

        /// <summary>
        /// 全データクリア
        /// </summary>
        public void clear()
        {
            mMatrix = ylib.unitMatrix(4);
            mSurfaceList = new List<Surface>();
        }

        /// <summary>
        /// マトリックス(配置と姿勢)クリア
        /// </summary>
        public void matrixClear()
        {
            mMatrix = ylib.unitMatrix(4);
        }

        /// <summary>
        /// マトリックスの追加
        /// </summary>
        /// <param name="mp">3Dマトリックス</param>
        public void addMatrix(double[,] mp)
        {
            mMatrix = ylib.matrixMulti(mMatrix, mp);
        }

        /// <summary>
        /// 配置をマトリックスに追加
        /// </summary>
        /// <param name="v">移動ベクトル</param>
        public void addTranslate(Point3D v)
        {
            mMatrix = ylib.matrixMulti(mMatrix, ylib.translate3DMatrix(v.x, v.y, v.z));
        }

        /// <summary>
        /// X軸回転をマトリックスに追加
        /// </summary>
        /// <param name="th">X軸回転角(rad)</param>
        public void addRotateX(double th)
        {
            mMatrix = ylib.matrixMulti(mMatrix, ylib.rotateX3DMatrix(th));
        }

        /// <summary>
        /// Y軸回転をマトリックスに追加
        /// </summary>
        /// <param name="th">Y軸回転角(rad)</param>
        public void addRotateY(double th)
        {
            mMatrix = ylib.matrixMulti(mMatrix, ylib.rotateY3DMatrix(th));
        }

        /// <summary>
        /// Z軸回転をマトリックスに追加
        /// </summary>
        /// <param name="th">Z軸回転角(rad)</param>
        public void addRotateZ(double th)
        {
            mMatrix = ylib.matrixMulti(mMatrix, ylib.rotateZ3DMatrix(th));
        }

        /// <summary>
        /// 拡大・縮小をマトリックスに追加
        /// </summary>
        /// <param name="v">拡大縮小率</param>
        public void addScale(Point3D v)
        {
            mMatrix = ylib.matrixMulti(mMatrix, ylib.scale3DMatrix(v.x, v.y, v.z));
        }


        public void addPrimitive()
        {
            addVertex(mPrimitive.createVertexList(), mPrimitive.mElelmentType, mPrimitive.mFaceColors);   
        }

        /// <summary>
        /// 各エレメントデータをLineまたはPolygonにして座標データを登録
        /// </summary>
        /// <param name="ps">座標リスト</param>
        /// <param name="elementType">要素種別</param>
        /// <param name="colors">色</param>
        public void addVertex(List<Point3D> ps, ELEMENTTYPE elementType, List<Brush> colors)
        {
            mSurfaceList = new List<Surface>();
            List<Point3D> buf = new List<Point3D>();
            int colorCount = 0;
            for (int i = 0; i < ps.Count; i++) {
                buf.Add(ps[i]);
                if (elementType == ELEMENTTYPE.LINES) {
                    if (buf.Count == 2) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                    }
                } else if (elementType == ELEMENTTYPE.LINE_STRIP) {
                    if (buf.Count == 2) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                        buf.Add(ps[i]);
                    }
                } else if (elementType == ELEMENTTYPE.LINE_LOOP) {
                    if (buf.Count == 2) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                        buf.Add(ps[i]);
                        if (i == ps.Count - 1) {
                            buf.Add(ps[0]);
                            surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                            mSurfaceList.Add(surface);
                        }
                    }
                } else if (elementType == ELEMENTTYPE.TRIANGLES) {
                    if (buf.Count == 3) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                    }
                } else if (elementType == ELEMENTTYPE.QUADS) {
                    if (buf.Count == 4) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                    }
                } else if (elementType == ELEMENTTYPE.TRIANGLE_STRIP) {
                    if (buf.Count == 3) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                        buf.Add(ps[i - 1]);
                        buf.Add(ps[i]);
                    }
                } else if (elementType == ELEMENTTYPE.QUAD_STRIP) {
                    if (buf.Count == 4) {
                        Point3D tp = buf[3];
                        buf[3] = buf[2];
                        buf[2] = tp;
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                        buf.Add(ps[i - 1]);
                        buf.Add(ps[i]);
                    }
                } else if (elementType == ELEMENTTYPE.TRIANGLE_FAN) {
                    if (buf.Count == 3) {
                        Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                        mSurfaceList.Add(surface);
                        buf = new List<Point3D>();
                        buf.Add(ps[0]);
                    }
                }
            }
            if (elementType == ELEMENTTYPE.POLYGON) {
                Surface surface = new Surface(buf, colors[colorCount++ % colors.Count]);
                mSurfaceList.Add(surface);
            }
        }

        /// <summary>
        /// 要素データ(サーフェス)をY3DDrawに登録
        /// </summary>
        /// <param name="y3Ddraw">Y3DDraw</param>
        /// <param name="addMatrix">変換マトリックス</param>
        public void setDrawData(Y3DDraw y3Ddraw, double[,] addMatrix)
        {
            double[,] matrix = ylib.matrixMulti(mMatrix, addMatrix);
            for (int i = 0; i < mSurfaceList.Count; i++) {
                List<Point3D> plist = new List<Point3D>();
                for (int j = 0; j < mSurfaceList[i].mCoordList.Count; j++) {
                    plist.Add(mSurfaceList[i].mCoordList[j].toMatrix(matrix));
                }
                y3Ddraw.addSurfaceList(plist, mSurfaceList[i].mFillColor);
            }
        }

        /// <summary>
        /// 要素データをSurfaceデータに変換
        /// </summary>
        /// <param name="addMatrix">変換マトリックス</param>
        /// <returns>Surfaceリスト</returns>
        public List<Surface> cnvDrawData(double[,] addMatrix)
        {
            List<Surface> surfaceList = new List<Surface>();
            double[,] matrix = ylib.matrixMulti(mMatrix, addMatrix);
            for (int i = 0; i < mSurfaceList.Count; i++) {
                List<Point3D> plist = new List<Point3D>();
                for (int j = 0; j < mSurfaceList[i].mCoordList.Count; j++) {
                    plist.Add(mSurfaceList[i].mCoordList[j].toMatrix(matrix));
                }
                surfaceList.Add(new Surface(plist, mSurfaceList[i].mFillColor));
            }
            return surfaceList;
        }
    }
}
