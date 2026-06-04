using System.Windows.Forms;

namespace EvoLib.Utility
{
    /// <summary>
    /// Windows 標準のメッセージボックス（MessageBox.Show）を<br/>
    /// Unity から簡単に呼び出すためのユーティリティクラス。<br/>
    /// <br/>
    /// MesBoxInfo にメッセージ内容・タイトル・ボタン種類・アイコンを設定し、<br/>
    /// Open() を呼び出すだけでメッセージボックスを表示できる。<br/>
    /// <br/>
    /// 主な機能：<br/>
    /// ・MessageBoxButtons / MessageBoxIcon を指定したメッセージ表示<br/>
    /// ・タイトル・本文を自由に設定可能<br/>
    /// ・UnityEditor / Windows ビルド環境で利用可能（Windows.Forms 使用）<br/>
    /// <br/>
    /// デバッグ用の警告表示や、確認ダイアログを簡易的に実装したい場合に使用する。<br/>
    /// 参考記事：https://raspberly.hateblo.jp/entry/WindowsMessageBox
    /// </summary>
    public static class MsgDialogBox
    {
        /// <summary>
        /// メッセージボックス情報
        /// </summary>
        public class MesBoxInfo
        {
            /// <summary>
            /// メッセージテキスト
            /// </summary>
            public string msgText = "";
            /// <summary>
            /// タイトルテキスト
            /// </summary>
            public string titleText = "";
            /// <summary>
            /// メッセージボタン(デフォ：OK)
            /// </summary>
            public MessageBoxButtons buttons = MessageBoxButtons.OK;
            /// <summary>
            /// アイコン(デフォ：Error)
            /// </summary>
            public MessageBoxIcon icon = MessageBoxIcon.Error;
        }

        /// <summary>
        /// Windows標準のメッセージボックス表示
        /// </summary>
        /// <param name="mesBoxInfo">メッセージボックス情報</param>
        public static void Open(MesBoxInfo mesBoxInfo)
        {
            MessageBox.Show
            (
                mesBoxInfo.msgText,
                mesBoxInfo.titleText,
                mesBoxInfo.buttons,
                mesBoxInfo.icon
            );
        }
    }
}