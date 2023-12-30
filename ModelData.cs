using CoreLib;
using System.IO;
using System.Windows.Media;

namespace Note3DApp
{
    /// <summary>
    /// Pickしたデータの構造体
    /// </summary>
    public class PickData
    {
        public Element mElement;
        public PointD mPos;
        public DISPMODE mDispMode;

        public PickData(Element element, PointD pos, DISPMODE dispMode)
        {
            mElement = element;
            mPos = pos;
            mDispMode = dispMode;
        }
    }

    /// <summary>
    /// モデルデータ管理
    /// 
    /// ModelData()
    /// ModelData(Y3DDraw draw)
    /// void addParts(string name)                              カレントPartsにPartsの新規追加
    /// void addElement(string name)                            カレントPartsにElementを追加
    /// int addPickElement(Element element, PointD pos, DISPMODE face)  PickしたElementの追加
    /// void removeItem(int index)                              Indexで指定されたPartsまたはElementを削除する
    /// Parts searchIndexParts(int index)                       PartsをIndexでRootから検索
    /// Parts searchIndexParts(Parts parts, int index)          PartsをIndexで指定Partsから検索
    /// Element searchIndexElement(int index)                   ElementをRootからindexで検索
    /// Element searchIndexElement(Parts parts, int index)      ElementをIndexで指定Partsから検索
    /// int reIndex(Parts parts, int index)                     インテックスNoをつけ直す
    /// List<int> findIndex(Parts parts, double[,] matrix, Box b, PRIMITIVEFACE face)   PartsからPick対象のElementを検索
    /// void changeColor(Element element)                       エレメントの色変更
    /// void addLine(PointD sp, PointD ep)                      カレントPartsに線分追加
    /// void addArc(PointD sp, PointD ep)                       カレントPartsに円追加
    /// void addArc(PointD sp, PointD mp, PointD ep)            カレントPartsに円弧を追加
    /// void addRect(PointD sp, PointD ep)                      カレントPartsに四角(Polygon)追加
    /// void addPolygon(List<PointD> plist)                     カレントPartsにPolygon追加
    /// void addWireCube(PointD sp, PointD ep, double h = 1)    カレントPartsに立体枠(WireCube)を追加
    /// addCube(PointD sp, PointD ep, double h = 1)             カレントPartsに立方体(Cube)を追加
    /// void loadData(string path)                              ファイルから読み込む
    /// void saveData(string path)                              ファイルに保存
    /// 
    /// </summary>
    public class ModelData
    {
        public Y3DDraw mDraw;                           //  グラフィックライブラリ
        public Parts mRootParts;                        //  ルートデータ
        public Parts mCurParts;                         //  カレントPartsデータ
        public Element mCurElement;                     //  カレントElementデータ
        public DISPMODE mFace = DISPMODE.XY;      //  Primitive 作成面
        public int mIndex = 0;                          //  データ寸デックス
        public List<PickData> mPickElement = new List<PickData>();

        public Brush mPrimitiveBrush = Brushes.Green;
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ModelData()
        {
            mRootParts = new Parts();
            mRootParts.mName = "Root";
            mRootParts.mIndex = mIndex++;
            mCurParts = mRootParts;
            mCurParts.mElements = new List<Element>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="draw"></param>
        public ModelData(Y3DDraw draw)
        {
            mDraw = draw;
            mRootParts = new Parts();
            mRootParts.mName = "Root";
            mRootParts.mIndex = mIndex++;
            mCurParts = mRootParts;
            mCurParts.mElements = new List<Element>();
        }

        /// <summary>
        /// カレントPartsにPartsの新規追加
        /// </summary>
        /// <param name="name">Parts名</param>
        public void addParts(string name)
        {
            Parts parts = new Parts();
            parts.mName = name;
            mCurParts.add(parts);
            mCurParts = parts;
            mCurParts.mIndex = mIndex++;
        }

        /// <summary>
        /// カレントPartsにElementを追加
        /// </summary>
        /// <param name="name"></param>
        public void addElement(string name)
        {
            Element element = new Element();
            element.mName = name;
            mCurParts.add(element);
            mCurElement = element;
            mCurElement.mIndex = mIndex++;
        }

        /// <summary>
        /// PickしたElementの追加
        /// </summary>
        /// <param name="element"></param>
        /// <param name="pos"></param>
        /// <param name="face"></param>
        /// <returns></returns>
        public int addPickElement(Element element, PointD pos, DISPMODE face)
        {
            mPickElement.Add(new PickData(element, pos, face));
            return mPickElement.Count;
        }

        /// <summary>
        /// Indexで指定されたPartsまたはElementを削除する
        /// </summary>
        /// <param name="index"></param>
        public void removeItem(int index)
        {
            Parts parts = searchIndexParts(index);
            if (parts != null) {
                if (parts.mIndex == index) {
                    if (mCurParts.mIndex == index)
                        mCurParts = parts.mParent;
                    Parts parent = parts.mParent;
                    if (parent != null) {
                        for (int i = 0; i < parent.mParts.Count; i++) {
                            if (parent.mParts[i].mIndex == index) {
                                parent.mParts.RemoveAt(i);
                                break;
                            }
                        }
                    }
                } else {
                    for (int i = 0; i < parts.mElements.Count; i++) {
                        if (parts.mElements[i].mIndex == index) {
                            parts.mElements.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// PartsをIndexでRootから検索
        /// </summary>
        /// <param name="index">IndexNo</param>
        /// <returns>Parts</returns>
        public Parts searchIndexParts(int index)
        {
            return searchIndexParts(mRootParts, index);
        }

        /// <summary>
        /// PartsをIndexで指定Partsから検索
        /// </summary>
        /// <param name="parts">Parts</param>
        /// <param name="index">IndexNo</param>
        /// <returns>Parts</returns>
        public Parts searchIndexParts(Parts parts, int index)
        {
            if (parts.mIndex == index)
                return parts;
            for (int i = 0; i < parts.mElements.Count; i++) {
                if (parts.mElements[i].mIndex == index)
                    return parts;
            }
            for (int i = 0; i < parts.mParts.Count; i++) {
                Parts result = searchIndexParts(parts.mParts[i], index);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// ElementをRootからindexで検索
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>Element</returns>
        public Element searchIndexElement(int index)
        {
            return searchIndexElement(mRootParts, index);
        }

        /// <summary>
        /// ElementをIndexで指定Partsから検索
        /// </summary>
        /// <param name="parts">Parts</param>
        /// <param name="index">IndexNo</param>
        /// <returns>Element</returns>
        public Element searchIndexElement(Parts parts, int index)
        {
            if (parts.mIndex == index)
                return null;
            for (int i = 0; i < parts.mElements.Count; i++) {
                if (parts.mElements[i].mIndex == index)
                    return parts.mElements[i];
            }
            for (int i = 0; i < parts.mParts.Count; i++) {
                Element result = searchIndexElement(parts.mParts[i], index);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// インテックスNoをつけ直す
        /// </summary>
        /// <param name="parts">Parts</param>
        /// <param name="index">インデックスNo</param>
        /// <returns>インデックスNo</returns>
        public int reIndex(Parts parts, int index)
        {
            parts.mIndex = index++; ;
            for (int i = 0; i < parts.mElements.Count; i++) {
                parts.mElements[i].mIndex = index++; ;
            }
            for (int i = 0; i < parts.mParts.Count; i++) {
                index = parts.mParts[i].reIndex(index);
            }
            return index;
        }

        /// <summary>
        /// PartsからPick対象のElementを検索する
        /// </summary>
        /// <param name="parts">Parts</param>
        /// <param name="matrix">変換マトリックス</param>
        /// <param name="b">検索Box</param>
        /// <returns>検索リスト</returns>
        //public List<int> findIndex(Parts parts, double[,] matrix, Box b, DISPMODE face)
        //{
        //    List<int> picks = new List<int>();
        //    matrix = ylib.matrixMulti(matrix, parts.mMatrix);
        //    for (int i = 0; i < parts.mParts.Count; i++)
        //        picks.AddRange(findIndex(parts.mParts[i], matrix, b, face));
        //    for (int i = 0; i < parts.mElements.Count; i++) {
        //        matrix = ylib.matrixMulti(matrix, parts.mElements[i].mMatrix);
        //        if (parts.mElements[i].mPrimitive.pickChk(b, matrix, face))
        //            picks.Add(parts.mElements[i].mIndex);
        //    }
        //    return picks;
        //}

        /// <summary>
        /// PartsからPick対象要素を
        /// </summary>
        /// <param name="parts">Parts</param>
        /// <param name="b">検索Box</param>
        /// <param name="face">表示面</param>
        /// <returns>検索リスト</returns>
        public List<int> findIndex(Parts parts, Box b, DISPMODE face)
        {
            List<int> picks = new List<int>();
            picks = parts.pickChk(b, ylib.unitMatrix(4), face);
            return picks;
        }

        /// <summary>
        /// エレメントの色変更
        /// </summary>
        /// <param name="element"></param>
        public void changeColor(Element element)
        {
            element.mPrimitive.mLineColor = mPrimitiveBrush;
            element.mPrimitive.mFaceColors[0] = mPrimitiveBrush;
            element.update3DData();
        }

        /// <summary>
        /// カレントPartsに線分追加
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        public void addLine(PointD sp, PointD ep)
        {
            LinePrimitive line = new LinePrimitive(sp, ep, mFace);
            line.mLineColor = mPrimitiveBrush;
            line.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = line.createElement();
            mCurElement.mPrimitive = line;
            mCurElement.mName = "LINE";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsに円追加
        /// </summary>
        /// <param name="sp">始点(中心点)</param>
        /// <param name="ep">終点(円弧上の点)</param>
        public void addArc(PointD sp, PointD ep)
        {
            ArcPrimitive arc = new ArcPrimitive(new ArcD(sp, sp.length(ep)), mFace);
            arc.mLineColor = mPrimitiveBrush;
            arc.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = arc.createElement();
            mCurElement.mPrimitive = arc;
            mCurElement.mName = "ARC";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsに円弧を追加
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="mp"></param>
        /// <param name="ep"></param>
        public void addArc(PointD sp, PointD mp, PointD ep)
        {
            ArcPrimitive arc = new ArcPrimitive(new ArcD(sp, mp, ep), mFace);
            arc.mLineColor = mPrimitiveBrush;
            arc.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = arc.createElement();
            mCurElement.mPrimitive = arc;
            mCurElement.mName = "ARC";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsに四角(Polygon)追加
        /// </summary>
        /// <param name="sp">始点(対角点)</param>
        /// <param name="ep">終点(対角点)</param>
        public void addRect(PointD sp, PointD ep)
        {
            List<PointD> plist = new List<PointD>() {
                sp, new PointD(sp.x, ep.y), ep, new PointD(ep.x, sp.y)
            };
            PolygonPrimitive polygon = new PolygonPrimitive(plist, mFace);
            polygon.mLineColor = mPrimitiveBrush;
            polygon.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = polygon.createElement();
            mCurElement.mPrimitive = polygon;
            mCurElement.mName = "POLYGON";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsにPolygon追加
        /// </summary>
        /// <param name="plist">座標リスト</param>
        public void addPolygon(List<PointD> plist)
        {
            PolygonPrimitive polygon = new PolygonPrimitive(plist, mFace);
            polygon.mLineColor = mPrimitiveBrush;
            polygon.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = polygon.createElement();
            mCurElement.mPrimitive = polygon;
            mCurElement.mName = "POLYGON";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsに立体枠(WireCube)を追加
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        public void addWireCube(PointD sp, PointD ep, double h = 1)
        {
            WireCubePrimitive wireCube = new WireCubePrimitive(sp, ep, h, mFace);
            wireCube.mLineColor = mPrimitiveBrush;
            wireCube.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = wireCube.createElement();
            mCurElement.mPrimitive = wireCube;
            mCurElement.mName = "WIRECUBE";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// カレントPartsに立方体(Cube)を追加
        /// </summary>
        /// <param name="sp">始点</param>
        /// <param name="ep">終点</param>
        public void addCube(PointD sp, PointD ep, double h = 1)
        {
            CubePrimitive cube = new CubePrimitive(sp, ep, h, mFace);
            cube.mLineColor = mPrimitiveBrush;
            cube.mFaceColors[0] = mPrimitiveBrush;
            mCurElement = cube.createElement();
            mCurElement.mPrimitive = cube;
            mCurElement.mName = "CUBE";
            mCurElement.mIndex = mIndex++;
            mCurParts.add(mCurElement);
        }

        /// <summary>
        /// ファイルから読み込む
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void loadData(string path)
        {
            if (!File.Exists(path))
                return;
            List<string[]> dataList = ylib.loadCsvData(path);
            int sp = 0;
            while (sp < dataList.Count) {
                string[] buf = dataList[sp++];
                if (buf[0] == "Parts") {
                    mRootParts = new Parts(buf[1], ylib.intParse(buf[2]));
                    sp = mRootParts.setDataList(dataList, sp);
                } else if (buf[0] == "PartsEnd") {
                    break;
                }
            }
            mIndex = mRootParts.reIndex(0);
            mCurParts = mRootParts;
            mCurElement = null;
        }

        /// <summary>
        /// ファイルに保存
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void saveData(string path)
        {
            if (path.Length == 0)
                return;
            List<string[]> dataList = new List<string[]>();
            dataList.AddRange(mRootParts.toDataList());
            ylib.saveCsvData(path, dataList);
        }
    }
}
