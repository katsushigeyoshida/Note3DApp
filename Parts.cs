using CoreLib;

namespace Note3DApp
{
    /// <summary>
    /// Parts    パーツクラス　エレメントとパーツの集合クラス
    ///     Parts()
    ///     Parts(string name, int index)
    ///     Parts toCopy()                  
    ///     string toString()                           パーツ情報
    ///     List<string[]> toDataList()                 Partsデータを文字列配列リストに変換
    ///     int setDataList(List<string[]> dataList, int sp)    文字列配列リストからPartsデータを設定
    ///     void clear()                                データのクリア
    ///     void add(Element element)                   要素の追加
    ///     void add(Parts part)                        パーツの追加
    ///     List<Surface> cnvDrawData(double[,] addMatrix)  要素データにSurfaceデータに変換
    ///     int reIndex(int index)                      インデックスNoを再設定する
    ///     void matrixClear()                          マトリックス(配置と姿勢)クリア
    ///     void addMatrix(double[,] mp)                マトリックスの追加
    ///     void addTranslate(Point3D v)                配置をマトリックスに追加
    ///     void addRotateX(double th)                  X軸回転をマトリックスに追加
    ///     void addRotateY(double th)                  Y軸回転をマトリックスに追加
    ///     void addRotateZ(double th)                  Z軸回転をマトリックスに追加
    ///     void addScale(Point3D v)                    拡大・縮小をマトリックスに追加
    /// </summary>
    public class Parts
    {
        public string mName;                            //  パーツ名称
        public int mIndex = -1;                         //  インデックス
        public double[,] mMatrix;                       //  配置と姿勢設定マトリックス
        public List<Parts> mParts { set; get; }         //  パーツリスト
        public List<Element> mElements { get; set; }    //  要素リスト
        public Parts? mParent = null;                   //  親パーツ


        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Parts()
        {
            mMatrix = ylib.unitMatrix(4);
            mElements = new List<Element>();
            mParts = new List<Parts>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">パーツ名称</param>
        /// <param name="index">インデックス</param>
        public Parts(string name, int index)
        {
            mName = name;
            mIndex = index;
            mMatrix = ylib.unitMatrix(4);
            mElements = new List<Element>();
            mParts = new List<Parts>();
        }

        /// <summary>
        /// コピーの作成(Partsリストの扱いが問題)
        /// </summary>
        /// <returns></returns>
        public Parts toCopy()
        {
            Parts parts = new Parts();
            parts.mElements = mElements.ConvertAll(P => P.toCopy());    //  Indexのふり直しが必要 ?
            //parts.mParts = mParts;      //  ディープコピーするのは問題、参照では管理できない
            parts.mMatrix = ylib.copyMatrix(mMatrix);
            parts.mName = mName;
            parts.mIndex = mIndex;
            parts.mParent = mParent;
            return parts;
        }

        /// <summary>
        /// パーツ情報
        /// </summary>
        /// <returns><文字列/returns>
        public string toString()
        {
            string buf = $"パーツ名:{mName} [{mIndex}]";
            buf += $"\nパーツリスト:";
            for (int i = 0; i < mParts.Count; i++)
                buf += " " + mParts[i].mName;
            buf += $"\nエレメントリスト:";
            for (int i = 0; i < mElements.Count; i++)
                buf += " " + mElements[i].mName;
            buf += $"\n移動: {mMatrix[3, 0]} {mMatrix[3, 1]} {mMatrix[3, 2]}";
            buf += $"\n拡大縮小: {mMatrix[0, 0]} {mMatrix[1, 1]} {mMatrix[2, 2]}";
            buf += $"\n回転: {ylib.R2D(Math.Asin(mMatrix[1, 2]))} {ylib.R2D(Math.Asin(-mMatrix[0, 2]))} {ylib.R2D(Math.Asin(mMatrix[1, 0]))}";
            return buf;
        }

        /// <summary>
        /// Partsデータを文字列配列リストに変換
        /// </summary>
        /// <returns>文字列配列リスト</returns>
        public List<string[]> toDataList()
        {
            List<string[]> dataList = new List<string[]>();
            string[] buf = { "Parts", mName, mIndex.ToString() };
            dataList.Add(buf);
            if (0 < mParts.Count) {
                foreach (var parts in mParts) {
                    dataList.AddRange(parts.toDataList());
                }
            }
            if (0 < mElements.Count) {
                foreach (var element in mElements) {
                    dataList.AddRange(element.toDataList());
                }
            }
            //  マトリックス
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
            dataList.Add(buf);

            buf = new string[] { "PartsEnd" };
            dataList.Add(buf);
            return dataList;
        }

        /// <summary>
        /// 文字列配列リストからPartsデータを設定
        /// </summary>
        /// <param name="dataList">文字列配列リスト</param>
        /// <param name="sp">リスト開始位置</param>
        /// <returns>リスト終了位置</returns>
        public int setDataList(List<string[]> dataList, int sp)
        {
            while (sp < dataList.Count) {
                string[] buf = dataList[sp++];
                if (buf[0] == "Parts") {
                    Parts parts = new Parts(buf[1], ylib.intParse(buf[2]));
                    sp = parts.setDataList(dataList, sp);
                    parts.mParent = this;
                    mParts.Add(parts);
                } else if (buf[0] == "Element") {
                    Element element = new Element(buf[1], ylib.intParse(buf[2]));
                    sp = element.setDataList(dataList, sp);
                    element.mParent = this;
                    mElements.Add(element);
                } else if (buf[0] == "Matrix") {
                    int row = ylib.intParse(buf[1]);
                    int col = ylib.intParse(buf[2]);
                    for (int i = 0; i < row; i++) {
                        for (int j = 0; j < col; j++) {
                            mMatrix[i, j] = ylib.doubleParse(buf[i * col + j + 3]);
                        }
                    }
                } else if (buf[0] == "PartsEnd") {
                    break;
                }
            }
            return sp;
        }

        /// <summary>
        /// 2D 表示
        /// </summary>
        /// <param name="draw"></param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        public void draw2D(Y3DDraw draw, double[,] mp, DISPMODE face)
        {
            mp = ylib.matrixMulti(mMatrix, mp);
            if (0 < mParts.Count) {
                foreach (var parts in mParts) {
                    parts.draw2D(draw, mp, face);
                }
            }
            if (0 < mElements.Count) {
                foreach (var element in mElements) {
                    element.draw2D(draw, mp, face);
                }
            }
        }

        /// <summary>
        /// 2Dでのピックリストの取得
        /// </summary>
        /// <param name="b">ピック領域</param>
        /// <param name="mp">変換マトリックス</param>
        /// <param name="face">表示面</param>
        /// <returns>ピックリスト</returns>
        public List<int> pickChk(Box b, double[,] mp, DISPMODE face)
        {
            List<int> indexList = new List<int>();
            mp = ylib.matrixMulti(mMatrix, mp);
            if (0 < mParts.Count) {
                foreach (var parts in mParts) {
                    indexList.AddRange(parts.pickChk(b, mp, face));
                }
            }
            if (0 < mElements.Count) {
                foreach (var element in mElements) {
                    int index = element.pickChk(b, mp, face);
                    if (0 <= index)
                        indexList.Add(index);
                }
            }
            return indexList;
        }

        /// <summary>
        /// データのクリア
        /// </summary>
        public void clear()
        {
            mMatrix = ylib.unitMatrix(4);
            mElements.Clear();
            mParts.Clear();
        }

        /// <summary>
        /// 要素の追加
        /// </summary>
        /// <param name="element">要素</param>
        public void add(Element element)
        {
            if (mElements == null)
                mElements = new List<Element>();
            element.mParent = this;
            mElements.Add(element);
        }

        /// <summary>
        /// パーツの追加
        /// </summary>
        /// <param name="part">パーツ</param>
        public void add(Parts part)
        {
            if (mParts == null)
                mParts = new List<Parts>();
            part.mParent = this;
            mParts.Add(part);
        }

        /// <summary>
        /// Elementの削除
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool remove(int index)
        {
            for (int i = 0; i < mElements.Count; i++) {
                if (mElements[i].mIndex == index) {
                    mElements.RemoveAt(i);
                    return true;
                }
            }
            for (int i = 0; i < mParts.Count; i++) {
                if (mParts[i].remove(index))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 要素データを表示用のSurfaceデータに座標変換
        /// </summary>
        /// <param name="addMatrix">変換マトリックス</param>
        /// <returns>Surfaceリスト</returns>
        public List<Surface> cnvDrawData(double[,] addMatrix)
        {
            List<Surface> surfaceList = new List<Surface>();
            double[,] matrix = ylib.matrixMulti(mMatrix, addMatrix);
            for (int i = 0; i < mElements.Count; i++) {
                surfaceList.AddRange(mElements[i].cnvDrawData(matrix));
            }
            for (int i = 0; i < mParts.Count; i++) {
                surfaceList.AddRange(mParts[i].cnvDrawData(matrix));
            }
            return surfaceList;
        }

        /// <summary>
        /// インデックスNoを再設定する
        /// </summary>
        /// <param name="index">開始インデックスNo</param>
        /// <returns>終了インデックスNo</returns>
        public int reIndex(int index)
        {
            mIndex = index++;
            for (int i = 0; i < mElements.Count; i++) {
                mElements[i].mIndex = index++;
            }
            for(int i = 0; i < mParts.Count; i++) {
                index = mParts[i].reIndex(index);
            }
            return index;
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
    }
}
