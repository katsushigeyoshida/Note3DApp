using CoreLib;

namespace Note3DApp
{
    /// <summary>
    /// Parts    パーツクラス　エレメントとパーツの集合クラス
    ///     Parts()
    ///     void clear()                    データのクリア
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
        public List<Element> mElements { get; set; }    //  要素リスト
        public List<Parts> mParts { set; get; }         //  パーツリスト
        public double[,] mMatrix;                       //  配置と姿勢設定マトリックス
        public string mName;                            //  パーツ名称
        public int mIndex = -1;                         //  インデックス
        public Parts? mParent = null;                   //  親パーツ


        private YLib ylib = new YLib();

        public Parts()
        {
            mMatrix = ylib.unitMatrix(4);
            mElements = new List<Element>();
            mParts = new List<Parts>();
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
