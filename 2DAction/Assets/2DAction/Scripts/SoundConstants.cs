namespace KemonoR.Constants
{
	public static class Sound
	{
		public enum BGM
		{
			STOP = -1,
			NONE = 0,

			BGM01 = 1,
			BGM02 = 2,
			BGM03 = 3,
			BGM04 = 4,
			BGM05 = 5,

			MENU = BGM01,
			FIELD = BGM02,
			EVENT = BGM03,
			EXPLORE = BGM04,
			TITLE = BGM05,
		}

		public enum JINGLE
		{
			JINGLE01 = 1,
			JINGLE02,
			JINGLE03,
			JINGLE04,

            JINGLE_SUCCESS = JINGLE01,
            JINGLE_GREAT = JINGLE02,
			JINGLE_FAILURE = JINGLE03,
            JINGLE_ANALYSIS = JINGLE04,
		}

		public enum SE
        {
            SE001 = 1,
            SE002,
            SE003,
            SE004,
            SE005,
            SE006,
            SE007,
            SE008,
            SE009,
            SE010,
            SE011,
            SE012,
            SE013,
            SE014,
            SE101 = 101,
            SE102,
            SE103,
            SE104,
            SE105,
            SE106,
            SE107,
            SE108,
            SE109,
            SE110,
            SE111,
            SE112,
            SE113,
            SE114,
            SE115,
            SE116,
            SE117,
            SE118,
            SE119,
            SE120,
            SE121,
            SE122,
            SE123,
            SE124,
            SE125,
            SE126,
            SE127,
            SE128,
            SE129,
            SE130,
            SE131,
            SE132,
            SE133,

            SYSSE_DECIDE = SE001,
            SYSSE_CANCEL = SE002,
            SYSSE_SELECT = SE003,
			SYSSE_ERROR = SE004,
			SYSSE_CHANGE_TAB = SE005,
			SYSSE_USE_ITEM = SE006,
			SYSSE_ALERT = SE007,
            SYSSE_REMOVE_EQUIP = SE008,
		}

		public static string GetBGM(BGM bgm)
		{
			return $"BGM{(int)bgm:D2}";
		}

		public static string GetSE(SE se)
		{
			return $"SE{(int)se:D3}";
		}

		public static string GetJingle(JINGLE jingle)
		{
			return $"JINGLE{(int)jingle:D2}";
		}
	}
}
