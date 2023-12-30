﻿using CoreLib;
using System.IO;
using System.Windows;

namespace Note3DApp
{
    public class FileData
    {
        public string mBaseDataFolder = "3DModel";
        public string mGenreName = "モデル";
        public string mDataName = "無題";
        public string mDataExt = ".csv";
        public Window mMainWindow;

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="window"></param>
        public FileData(Window window)
        {
            mMainWindow = window;
        }

        /// <summary>
        /// ベースフォルダの設定
        /// </summary>
        /// <param name="baseDataFolder"></param>
        public void setBaseDataFolder(string baseDataFolder = "")
        {
            try {
                if (baseDataFolder != "")
                    mBaseDataFolder = baseDataFolder;
                if (!Directory.Exists(mBaseDataFolder))
                    Directory.CreateDirectory(mBaseDataFolder);
                string genreFolder = Path.Combine(mBaseDataFolder, mGenreName);
                if (!Directory.Exists(genreFolder))
                    Directory.CreateDirectory(genreFolder);
            } catch (Exception e) {
                ylib.messageBox(mMainWindow, e.Message);
            }
        }

        /// <summary>
        /// ジャンルフォルダの設定
        /// </summary>
        /// <param name="genreName">ジャンル名</param>
        public void setGenreFolder(string genreName)
        {
            mGenreName = genreName;
            string genreFolder = getCurGenrePath();
            if (!Directory.Exists(genreFolder))
                Directory.CreateDirectory(genreFolder);
        }

        /// <summary>
        /// ジャンルの追加
        /// </summary>
        /// <returns></returns>
        public string addGenre()
        {
            InputBox dlg = new InputBox();
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Title = "ジャンル追加";
            if (dlg.ShowDialog() == true) {
                string genrePath = getGenrePath(dlg.mEditText.ToString());
                if (Directory.Exists(genrePath)) {
                    ylib.messageBox(mMainWindow, "すでにジャンルフォルダが存在しています。");
                } else {
                    Directory.CreateDirectory(genrePath);
                    return dlg.mEditText.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// ジャンル名の変更
        /// </summary>
        /// <param name="genre">ジャンル名</param>
        /// <returns></returns>
        public string renameGenre(string genre)
        {
            InputBox dlg = new InputBox();
            dlg.Title = "ジャンル名変更";
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mEditText = genre;
            string oldGenrePath = getGenrePath(genre);
            if (dlg.ShowDialog() == true) {
                string newGenrePath = getGenrePath(dlg.mEditText.ToString());
                if (Directory.Exists(newGenrePath)) {
                    ylib.messageBox(mMainWindow, "すでにジャンルフォルダが存在しています。");
                } else {
                    Directory.Move(oldGenrePath, newGenrePath);
                    return dlg.mEditText.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// ジャンルの削除
        /// </summary>
        /// <param name="genre"></param>
        /// <returns></returns>
        public bool removeGenre(string genre)
        {
            string genrePath = getGenrePath(genre);
            if (ylib.messageBox(mMainWindow, genre + " を削除します", "", "項目削除", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Directory.Delete(genrePath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 図面の追加
        /// </summary>
        /// <returns></returns>
        public string addItem()
        {
            InputBox dlg = new InputBox();
            dlg.Title = "図面追加";
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (dlg.ShowDialog() == true) {
                string filePath = getItemFilePath(dlg.mEditText.ToString());
                if (File.Exists(filePath)) {
                    ylib.messageBox(mMainWindow, "すでにファイルが存在しています。");
                } else {
                    return dlg.mEditText.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// 図面名の変更
        /// </summary>
        /// <param name="dataName"></param>
        /// <returns></returns>
        public string renameItem(string dataName)
        {
            InputBox dlg = new InputBox();
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Title = "図面名変更";
            dlg.mEditText = dataName;
            string oldDataFilePath = getItemFilePath(dataName);
            if (dlg.ShowDialog() == true) {
                string newDataFilePath = getItemFilePath(dlg.mEditText.ToString());
                if (File.Exists(newDataFilePath)) {
                    ylib.messageBox(mMainWindow, "すでにファイルが存在しています。");
                } else {
                    File.Move(oldDataFilePath, newDataFilePath);
                    return dlg.mEditText.ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// 図面ファイルの削除
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public bool removeItem(string itemName)
        {
            string filePath = getItemFilePath(itemName);
            if (ylib.messageBox(mMainWindow, itemName + " を削除します", "", "項目削除", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
#if false
        /// <summary>
        /// 図面ファイルのコピー/移動
        /// </summary>
        /// <param name="itemName">図面名</param>
        /// <param name="move">移動の可否</param>
        /// <returns></returns>
        public bool copyItem(string itemName, bool move = false)
        {
            SelectCategory dlg = new SelectCategory();
            dlg.Owner = mMainWindow;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mRootFolder = mBaseDataFolder;
            if (dlg.ShowDialog() == true) {
                string oldItemPath = getItemFilePath(itemName);
                string newItemPath = getItemFilePath(itemName, dlg.mSelectCategory, dlg.mSelectGenre);
                string item = Path.GetFileNameWithoutExtension(itemName);
                string ext = Path.GetExtension(itemName);
                int opt = 1;
                while (File.Exists(newItemPath)) {
                    newItemPath = getItemFilePath(item + "(" + opt + ")", dlg.mSelectCategory, dlg.mSelectGenre);
                    opt++;
                }
                if (move) {
                    File.Move(oldItemPath, newItemPath);
                } else {
                    File.Copy(oldItemPath, newItemPath);
                }
                return true;
            }
            return false;
        }
#endif
        /// <summary>
        /// ジャンルリストの取得
        /// </summary>
        /// <returns></returns>
        public List<string> getGenreList()
        {
            List<string> genreList = ylib.getDirectories(mBaseDataFolder);
            if (genreList != null) {
                genreList.Sort();
                genreList = genreList.ConvertAll(p => ylib.getLastFolder(p, 1));
            }
            return genreList;
        }

        /// <summary>
        /// データファイルの一覧の取得
        /// </summary>
        /// <returns></returns>
        public List<string> getItemFileList()
        {
            List<string> fileNameList = new List<string>();
            try {
                string[] files = ylib.getFiles(getCurGenrePath() + "\\*.csv");
                if (files != null) {
                    for (int i = 0; i < files.Length; i++) {
                        fileNameList.Add(Path.GetFileNameWithoutExtension(files[i]));
                    }
                }
            } catch (Exception e) {
                ylib.messageBox(mMainWindow, e.Message);
            }
            return fileNameList;
        }

        /// <summary>
        /// カレント状態のジャンルパスの取得
        /// </summary>
        /// <returns>ジャンルパス</returns>
        public string getCurGenrePath()
        {
            return Path.Combine(mBaseDataFolder, mGenreName);
        }

        /// <summary>
        /// カレント状態の図面ファイルパスの取得
        /// </summary>
        /// <returns>ファイルパス</returns>
        public string getCurItemFilePath()
        {
            return getItemFilePath(mDataName, mGenreName);
        }

        /// <summary>
        /// 大分類(ジャンル)パスの取得
        /// </summary>
        /// <param name="genre">大分類名</param>
        /// <returns>大分類パス</returns>
        public string getGenrePath(string genre)
        {
            return Path.Combine(mBaseDataFolder, genre);
        }

        /// <summary>
        /// 図面ファイル名を指定してファイルパスの取得
        /// </summary>
        /// <param name="dataName">図面ファイル名</param>
        /// <returns>ファイルパス</returns>
        public string getItemFilePath(string dataName)
        {
            return getItemFilePath(dataName, mGenreName);
        }

        /// <summary>
        /// 図面ファイル名とジャンルを指定してファイルパスを取得
        /// </summary>
        /// <param name="dataName">図面ファイル名</param>
        /// <param name="categoryName">カテゴリ名param>
        /// <param name="genreName">ジャンル</param>
        /// <returns>ファイルパス</returns>
        public string getItemFilePath(string dataName, string genreName)
        {
            string curDataFilePath = genreName + "\\" + dataName + mDataExt;
            return Path.Combine(mBaseDataFolder, curDataFilePath);
        }
    }
}
