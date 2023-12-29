using CoreLib;

namespace Note3DApp
{
    /// <summary>
    /// Parts    パーツクラス　エレメントとパーツの集合クラス
    ///     Parts()
    ///     void clear()                    データのクリア
    ///     string toString()               パーツ情報
    ///     void add(Element element)       要素の追加
    ///     void add(Parts part)            パーツの追加
    ///     List<Surface> cnvDrawData(double[,] addMatrix)  要素データにSurfaceデータに変換
    ///     void matrixClear()              マトリックス(配置と姿勢)クリア
    ///     void addMatrix(double[,] mp)    マトリックスの追加
    ///     void addTranslate(Point3D v)    配置をマトリックスに追加
    ///     void addRotateX(double th)      X軸回転をマトリックスに追加
    ///     void addRotateY(double th)      Y軸回転をマトリックスに追加
    ///     void addRotateZ(double th)      Z軸回転をマトリックスに追加
    ///     void addScale(Point3D v)        拡大・縮小をマトリックスに追加
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

        public Parts()
        {
            mMatrix = ylib.unitMatrix(4);
            mElements = new List<Element>();
            mParts = new List<Parts>();
        }

        public Parts toCopy()
        {
            Parts parts = new Parts();
            parts.mElements = mElements.ConvertAll(P => P.toCopy());    //  Indexのふり直しが必要 ?
            parts.mParts = mParts;      //  ディープコピーするのは問題、参照では管理できない
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

        public List<string[]> toDataList()
        {
            List<string[]> list = new List<string[]>();
            string[] buf = { "Parts", "Name", mName, "Index", mIndex.ToString() };
            list.Add(buf);
            if (0 < mParts.Count) {
                buf = new string[1 + mParts.Count * 2];
                buf[0] = "PartsList";
                for (int i = 1; i <= mParts.Count; i += 2) {
                    buf[i] = mParts[i].mName;
                    buf[i + 1] = mParts[i].mIndex.ToString();
                }
                list.Add(buf);
            }
            if (0 < mElements.Count) {
                buf = new string[1 + mElements.Count * 2];
                buf[0] = "ElementsList";
                for (int i = 1; i <= mElements.Count; i += 2) {
                    buf[i] = mElements[i].mName;
                    buf[i + 1] = mElements[i].mIndex.ToString();
                }
                list.Add(buf);
            }
            int row = mMatrix.GetLength(0);
            int col = mMatrix.GetLength(1);
            buf = new string[row * col + 1];
            buf[0] = "Matrix";
            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
                    buf[i * col + j + 1] = mMatrix[i, j].ToString();
                }
            }
            list.Add(buf);
            buf = new string[] { "End" };
            list.Add(buf);
            return list;
        }

        public void setDataList(List<string[]> list)
        {
            int ival;
            double val;
            foreach (var buf in list) {
                if (buf[0] == "Parts") {
                    mName = buf[1];
                    mIndex = ylib.string2int(buf[2]);
                } else if (buf[0] == "PartsList") {
                    for (int i = 1; i < buf.Length; i += 2) {
                        Parts parts = new Parts();
                        parts.mName = buf[i];
                        parts.mIndex = int.TryParse(buf[i + 1], out ival) ? ival : 0;
                        mParts.Add(parts);
                    }
                } else if (buf[0] == "ElementsList") {
                    for (int i = 1; i < buf.Length; i += 2) {
                        Element element = new Element();
                        element.mName = buf[i];
                        element.mIndex = int.TryParse(buf[i + 1], out ival) ? ival : 0;
                        mElements.Add(element);
                    }
                } else if (buf[0] == "Matrix") {
                    int row = mMatrix.GetLength(0);
                    int col = mMatrix.GetLength(1);
                    for (int i = 0; i < row; i++) {
                        for (int j = 0; j < col; j++) {
                            mMatrix[i, j] = double.TryParse(buf[i * col + j + 1], out val) ? val : 0;
                        }
                    }
                } else if (buf[0] == "End") {
                    break;
                }
            }
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
