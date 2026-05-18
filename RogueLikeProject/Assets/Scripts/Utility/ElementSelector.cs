using UnityEngine;
using UnityEngine.UIElements;
using Manager;


namespace Utility
{
    public class ElementSelector
    {
        /// <summary>
        /// 選択番号
        /// </summary>
        /// <param name="selectNumber"></param>
        /// <param name="selectMaxNumber"></param>
        /// <param name="maxColumns"></param>
        /// <returns></returns>
        public int Selection(int selectNumber,int selectMaxNumber, int maxColumns = 1)
        {
            // 選択番号
            if (selectMaxNumber > 0)
            {
                if (InputManager.Instance.IsRepeat(InputManager.BtnType.Right))
                {
                    int line = (selectNumber / maxColumns) + 1;
                    int max = (maxColumns * line) - 1;
                    max = Mathf.Clamp(max, 0, (selectMaxNumber - 1));

                    if (max <= selectNumber)
                    {
                        if (InputManager.Instance.IsTrig(InputManager.BtnType.Right))
                        {
                            // 同じ行内でループ
                            selectNumber = (selectNumber / maxColumns) * maxColumns + (selectNumber + 1) % maxColumns;
                            if (selectNumber >= selectMaxNumber)
                            {
                                // 同じ行の左端へ
                                selectNumber = (selectNumber / maxColumns) * maxColumns;
                            }
                        }
                    }
                    else
                    {
                        selectNumber++;
                    }
                }
                else if (InputManager.Instance.IsRepeat(InputManager.BtnType.Left))
                {
                    int line = (selectNumber / maxColumns);
                    int min = (maxColumns * line);
                    min = Mathf.Clamp(min, 0, (selectMaxNumber - 1));

                    if (min >= selectNumber)
                    {
                        if (InputManager.Instance.IsTrig(InputManager.BtnType.Left))
                        {
                            // 同じ行内でループ
                            var mod = selectNumber - 1;
                            mod = (mod + (mod < 0 ? maxColumns : 0)) % maxColumns;
                            selectNumber = (selectNumber / maxColumns) * maxColumns + mod;
                            if (selectNumber >= selectMaxNumber)
                            {
                                // 同じ行の最大へ
                                selectNumber = selectMaxNumber - 1;
                            }
                        }
                    }
                    else
                    {
                        selectNumber--;
                    }
                }
                else if (InputManager.Instance.IsRepeat(InputManager.BtnType.Down))
                {
                    int col = selectNumber % maxColumns;
                    int line = (selectMaxNumber - 1) / maxColumns;
                    int max = (maxColumns * line) + col;
                    max = Mathf.Clamp(max, 0, (selectMaxNumber - 1));

                    if (selectNumber >= max)
                    {
                        if (InputManager.Instance.IsTrig(InputManager.BtnType.Down))
                        {
                            // 先頭の行
                            selectNumber = col;
                        }
                    }
                    else
                    {
                        selectNumber += maxColumns;
                        selectNumber = Mathf.Clamp(selectNumber, 0, max);
                    }
                }
                else if (InputManager.Instance.IsRepeat(InputManager.BtnType.Up))
                {
                    int col = selectNumber % maxColumns;
                    int line = (selectMaxNumber - 1) / maxColumns;
                    int max = (maxColumns * line) + col;
                    max = Mathf.Clamp(max, 0, (selectMaxNumber - 1));

                    if (selectNumber <= col)
                    {
                        if (InputManager.Instance.IsTrig(InputManager.BtnType.Up))
                        {
                            // 最後の行
                            selectNumber = max;
                        }
                    }
                    else
                    {
                        selectNumber -= maxColumns;
                        selectNumber = Mathf.Clamp(selectNumber, 0, max);
                    }
                }
            }

            return selectNumber;
        }
    }
}