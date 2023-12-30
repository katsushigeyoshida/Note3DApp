using CoreLib;
using System.Windows.Media;

namespace Note3DApp
{
    /// <summary>
    /// Element    エレメントクラス サーフェスデータの集合クラス
    ///     Element()
    ///     void clear()                    全データクリア
    ///     Element toCopy()                ディープコピー
    ///     string toString()               エレメント情報
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
        public string mName;                    //  エレメント名称
        public int mIndex = -1;                 //  インデックス
        public double[,] mMatrix;               //  配置と姿勢設定マトリックス
        public Primitive mPrimitive;            //  プリミティブリスト
        public List<Surface> mSurfaceList;      //  サーフェスデータリスト(LINE/TRYANGLE)
        public Parts? mParent = null;           //  親パーツ

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Element()
        {
            clear();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">エレメント名</param>
        /// <param name="index">インデックス</param>
        public Element(string name, int index)
        {
            mName = name;
            mIndex = index;
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
        /// コピーの作成
        /// </summary>
        /// <returns></returns>
        public Element toCopy()
        {
            Element element = new Element();
            element.mPrimitive = mPrimitive;
            element.mSurfaceList = mSurfaceList.ConvertAll(p => p.toCopy());
            element.mMatrix = ylib.copyMatrix(mMatrix);
            element.mName = mName;
            element.mIndex = mIndex;
            element.mParent = mParent;
            return element;
        }

        /// <summary>
        /// エレメント情報
        /// </summary>
        /// <returns>文字列</returns>
        public string toString()
        {
            string buf = $"エレメント名:{mName} [{mIndex}]";
            buf += $"\nプリミティブ:{mPrimitive.mPrimitiveId} {mPrimitive.mElementType} {mPrimitive.mPrimitiveFace} ";
            buf += $"{ylib.getBrushName(mPrimitive.mLineColor)} {ylib.getBrushName(mPrimitive.mFaceColors[0])}";
            buf += $"\n移動: {mMatrix[3,0]} {mMatrix[3, 1]} {mMatrix[3, 2]}";
            buf += $"\n拡大縮小: {mMatrix[0, 0]} {mMatrix[1, 1]} {mMatrix[2, 2]}";
            buf += $"\n回転: {ylib.R2D(Math.Asin(mMatrix[1, 2]))} {ylib.R2D(Math.Asin(-mMatrix[0, 2]))} {ylib.R2D(Math.Asin(mMatrix[1, 0]))}";
            return buf;
        }

        /// <summary>
        /// Elementデータを文字列配列リストに変換
        /// </summary>
        /// <returns>文字列配列リスト</returns>
        public List<string[]> toDataList()
        {
            List<string[]> list = new List<string[]>();
            string[] buf = { "Element", mName, mIndex.ToString() };
            list.Add(buf);
            list.Add(mPrimitive.toPropertyList().ToArray());
            list.Add(mPrimitive.toDataList().ToArray());
            int row = mMatrix.GetLength(0);
            int col = mMatrix.GetLength(1);
            buf = new string[row * col + 3];
            buf[0] = "Matrix";
            buf[1] = row.ToString();
            buf[2] = col.ToString();
            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
                    buf[i * col + j + 3] = mMatrix[i, j].ToString();
                }
            }
            list.Add(buf);
            buf = new string[] { "ElementEnd" };
            list.Add(buf);
            return list;
        }

        /// <summary>
        /// 文字列配列データを変換してElementデータに設定
        /// </summary>
        /// <param name="dataList">文字列配列リスト</param>
        /// <param name="sp">リスト開始位置</param>
        /// <returns>リスト終了位置</returns>
        public int setDataList(List<string[]> dataList, int sp)
        {
            while (sp < dataList.Count) {
                string[] buf = dataList[sp++];
                if (buf[0] == "PrimitiveId") {
                    switch (buf[1]) {
                        case "Line":
                            LinePrimitive line = new LinePrimitive();
                            line.setPropertyList(buf);
                            buf = dataList[sp++];
                            line.setDataList(buf);
                            mPrimitive = line;
                            break;
                        case "Arc":
                            ArcPrimitive arc = new ArcPrimitive();
                            arc.setPropertyList(buf);
                            buf = dataList[sp++];
                            arc.setDataList(buf);
                            mPrimitive = arc;
                            break;
                        case "Polyline":
                            break;
                        case "Polygon":
                            PolygonPrimitive polygon = new PolygonPrimitive();
                            polygon.setPropertyList(buf);
                            buf = dataList[sp++];
                            polygon.setDataList(buf);
                            mPrimitive = polygon;
                            break;
                        case "WireCube":
                            WireCubePrimitive wireCube = new WireCubePrimitive();
                            wireCube.setPropertyList(buf);
                            buf = dataList[sp++];
                            wireCube.setDataList(buf);
                            mPrimitive = wireCube;
                            break;
                        case "Cube":
                            CubePrimitive cube = new CubePrimitive();
                            cube.setPropertyList(buf);
                            buf = dataList[sp++];
                            cube.setDataList(buf);
                            mPrimitive = cube;
                            break;

                    }
                    if (mPrimitive != null)
                        mPrimitive.createVertexList();
                } else if (buf[0] == "Matrix") {
                    int row = ylib.intParse(buf[1]);
                    int col = ylib.intParse(buf[2]);
                    for (int i = 0; i < row; i++) {
                        for (int j = 0; j < col; j++) {
                            mMatrix[i, j] = ylib.doubleParse(buf[i * col + j + 3]);
                        }
                    }
                } else if (buf[0] == "ElementEnd") {
                    break;
                }
            }
            addVertex(mPrimitive.mVertexList, mPrimitive.mElementType, mPrimitive.mFaceColors);
            return sp;
        }

        /// <summary>
        /// 2D表示処理
        /// </summary>
        /// <param name="draw"></param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            mp = ylib.matrixMulti(mMatrix, mp);
            mPrimitive.draw2D(draw, mp, face);
        }

        /// <summary>
        /// 検索BoxによるPickの有無
        /// </summary>
        /// <param name="b">検索Box</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>Pickの有無</returns>
        public int pickChk(Box b, double[,] mp, DISPMODE face)
        {
            mp = ylib.matrixMulti(mMatrix, mp);
            if (mPrimitive.pickChk(b, mp, face))
                return mIndex;
            return -1;
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

        /// <summary>
        /// 3D VertexListをUPDATEする
        /// </summary>
        public void update3DData()
        {
            addVertex(mPrimitive.mVertexList, mPrimitive.mElementType, mPrimitive.mFaceColors);
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
