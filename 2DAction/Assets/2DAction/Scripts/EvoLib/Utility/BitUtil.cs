namespace EvoLib.Utility
{
    /// <summary>
    /// ビット計算クラス
    /// </summary>
    public static class BitUtil
    {
        /// <summary>
        /// Bit追加
        /// </summary>
        /// <param name="table">Bitを保存している変数</param>
        /// <param name="index">位置指定</param>
        /// <returns>追加後のBitを保存している変数</returns>
        public static int Add(int table, int index)
        {
            // indexで指定された位置に1Bit代入する
            table |= (1 << index);
            return table;
        }

        /// <summary>
        /// Bit削除
        /// </summary>
        /// <param name="table">Bitを保存している変数</param>
        /// <param name="index">位置指定</param>
        /// <returns>削除後のBitを保存している変数</returns>
        public static int Remove(int table, int index)
        {
            // indexで指定された位置のBitを反転し0にする
            table &= ~(1 << index);
            return table;
        }

        /// <summary>
        /// 指定した位置のBitが立っているかどうか
        /// </summary>
        /// <param name="table">Bitを保存している変数</param>
        /// <param name="index">位置指定</param>
        /// <returns>指定位置のBitが立っているかのフラグ</returns>
        public static bool IsOn(int table, int index)
        {
            // 指定した位置のBitが立っているかどうか
            bool isStanding = (table & (1 << index)) != 0;
            return isStanding;
        }

        /// <summary>
        /// ビットテーブルをリセットし、必要であれば指定したビットを立てた状態で返す。
        /// </summary>
        /// <param name="initialIndex">
        /// リセット後に立てておきたいビット番号（null の場合は何も立てない）
        /// </param>
        /// <returns>リセットされたビットテーブル</returns>
        public static int Reset(int? initialIndex = null)
        {
            int bitTable = 0;

            if (initialIndex.HasValue)
            {
                bitTable = Add(bitTable, initialIndex.Value);
            }

            return bitTable;
        }
    }
}
