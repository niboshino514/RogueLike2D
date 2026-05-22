using System.Windows.Forms;

namespace Utility
{
    public class MsgDialogBox
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