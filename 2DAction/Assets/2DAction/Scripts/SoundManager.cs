using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GMLib.Utility;
using GMLib.Audio;
using Debug = GMLib.Debug;

using KemonoR.Constants;
using System.Diagnostics;

namespace KemonoR.Manager
{
    public class SoundManager : SingletonMonoBehaviour<SoundManager>
    {
        [SerializeField]
        AudioClip[] _systemAudioClips = null;

        // ボリュームグループ
        public enum VolumeGroup
        {
            MASTER = 0,
            BGM,
            SE,
            MAX
        };

        public static readonly float BMG_DEFAULT_FADE_TIME = 0.5f;
        public static readonly string GAME_BGM_PATH = ""; // ファイル名のみで登録しているのでフォルダPathは不要
        public static readonly string GAME_SE_PATH = ""; // ファイル名のみで登録しているのでフォルダPathは不要

        // 各ボリューム
        private float[] _volumes = null;

        // マネージャーのスタンスキャッシュ
        private AudioBgmManager _audioBGM;
        private AudioSeManager _audioSE;
        public AudioSeManager AudioSE { get { return _audioSE; } }


        // ログ出力制御 (trueでログ出力）
        // "UNITY_EDITOR"もしくは"DEVELOPMENT_BUILD"定義時にのみ有効。
        // 上記以外の場合は、ログ出力関数自体が抑制される。
        public bool LogEnable { get; set; }


        /// <summary>
        /// Awake
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _volumes = new float[(int)VolumeGroup.MAX];
            //DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            _audioBGM = AudioBgmManager.Instance;
            _audioSE = AudioSeManager.Instance;
            _audioBGM.Initialize(AudioLoader, AudioDisposer);
            _audioSE.Initialize(AudioLoader, AudioDisposer);
            _audioSE.LogEnable = false;

            // システム系SEの事前登録
            foreach (var clip in _systemAudioClips)
            {
                _audioSE.EntryAudioClip(GAME_SE_PATH + clip.name + ".wav", clip);
            }

            // ボリュームのデフォルト値
            // ※ゲーム側からすぐにでも上書きされるかな
            SetVolume(VolumeGroup.MASTER, 1.0f);
            SetVolume(VolumeGroup.BGM, 0.5f);
            SetVolume(VolumeGroup.SE, 0.5f);
        }


        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            Stop();
        }

        // AudioManager へ登録するAudioClip 読み込み用関数
        private async UniTask<AudioClip> AudioLoader(string path)
        {
            AudioClip clip = await ResourceLoader.Instance.LoadResourceAsync<AudioClip>(path);
            return clip;
        }

        // AudioManager へ登録するAudioClipの破棄処理用関数
        private void AudioDisposer(string path)
        {
            // システム系で登録されたAudioClipは、破棄処理の必要が無いので、
            // 合致する名前のAudioClipは無視。
            foreach (var clip in _systemAudioClips)
            {
                if (path.Contains(clip.name))
                {
                    return;
                }
            }
            ResourceLoader.Instance.Release<AudioClip>(path);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 特定フォルダ以下のSEを予め読み込んでキャッシュしておく
        /// </summary>
        /// <remarks>
        /// パッケージ化していない場合に使う想定。
        /// </remarks>
        /// ----------------------------------------------------------------------
        public async UniTask LoadAudioData()
        {
            // 指定ディレクトリ以下の全ファイル名（サブディレクトリ含む）を取得
            var basePath = ResourceLoader.Instance.GetBasePath();
            var baseDir = basePath + GAME_SE_PATH;
            try
            {
                Log("LoadAudioData Directory Read: " + baseDir);
                string[] files = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
                List<string> loadFiles = new List<string>();

                // 全ファイル読み込み
                foreach (var f in files)
                {
                    // .metaファイルや対応外のオーディオデータは無視
                    if (GetAudioType(f) == AudioType.UNKNOWN)
                    {
                        continue;
                    }
                    // ファイル名部分だけ取り出すと、GAME_SE_PATH以下のサブディレクトリに
                    // 対応出来ないので、basePath を除く処理にしている。
                    var path = (basePath != string.Empty) ? (f.Replace(basePath, "").Replace("\\", "/")) : (f.Replace("\\", "/"));
                    if (_audioSE.IsCached(path))
                    {
                        Log($"[{path}]のSEは既にキャッシュ済み");
                        continue;
                    }
                    loadFiles.Add(path);
                }
                await LoadAudioClipsSE(loadFiles);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定ファイルに記載されているSEを読み込む
        /// </summary>
        /// <remarks>
        /// パッケージ化(Addressable使用時）している場合は、これを使用して
        /// SEの事前読み込みを行う。
        /// </remarks>
        /// ----------------------------------------------------------------------
        public async UniTask LoadAudioDataFromList(string path)
        {
            var textAsset = await ResourceLoader.Instance.LoadResourceAsync<TextAsset>(path);
            if (textAsset == null)
            {
                Log($"LoadAudioDataFromList: [{path}] not found.");
                return;
            }
            var text = textAsset.text;
            // 行ごとの文字列に分割
            string[] lineBuf = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            List<string> pathList = new List<string>();

            string basePath = ResourceLoader.Instance.GetBasePath();
            if (basePath != string.Empty)
            {
                foreach (var s in lineBuf)
                {
                    if (s != string.Empty)
                    {
                        pathList.Add(s.Replace(basePath, "").Trim());
                    }
                }
            }
            else
            {
                foreach (var s in lineBuf)
                {
                    if (s != string.Empty)
                    {
                        pathList.Add(s.Trim());
                    }
                }
            }
            await LoadAudioClipsSE(pathList);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 登録したオーディオを開放する
        /// </summary>
        /// <remarks>
        /// 事前登録(LoadAudioData, LoadAudioDataFromList)されているSEを破棄。
        /// BGMは再生時に読み込み、停止時に破棄している。
        /// </remarks>
        /// ----------------------------------------------------------------------
        public void ReleaseAudioData()
        {
            _audioBGM.ReleaseCache();
            _audioSE.ReleaseCache();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定SEファイルをまとめて読み込む
        /// </summary>
        /// <param name="pathList">ファイル名のリスト</param>
        /// ----------------------------------------------------------------------
        public async UniTask LoadAudioClipsSE(List<string> pathList)
        {
            List<UniTask> list = new List<UniTask>(pathList.Count);
            foreach (var f in pathList)
            {
                var t = _audioSE.LoadAudioClip(f);
                list.Add(t);
            }
            await UniTask.WhenAll(list);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定グループの音量を設定する
        /// </summary>
        /// <param name="grp">グループ</param>
        /// <param name="volume">ボリューム値 (0～1.0f)</param>
        /// ----------------------------------------------------------------------
        public void SetVolume(VolumeGroup grp, float volume)
        {
            AssertEX.IsRangeTrue(volume, 0.0f, 1.0f);
            _volumes[(int)grp] = volume;

            if (grp != VolumeGroup.MASTER)
            {
                // MASTER以外の音量はMASTERの音量に依存するので
                // 設定する値は、個別の音量値にMASTERの音量値を乗算する。
                volume *= _volumes[(int)VolumeGroup.MASTER];
            }
            switch (grp)
            {
                case VolumeGroup.BGM:
                    _audioBGM.SetVolume(volume);
                    break;
                case VolumeGroup.SE:
                    _audioSE.SetVolume(volume);
                    break;
                case VolumeGroup.MASTER:
                    SetVolume(VolumeGroup.BGM, volume);
                    SetVolume(VolumeGroup.SE, volume);
                    break;
            }
        }

        public float GetVolume(VolumeGroup grp)
        {
            return _volumes[(int)grp];
        }


        /// ----------------------------------------------------------------------
        /// <summary>
        /// 全サウンド停止
        /// </summary>
        /// ----------------------------------------------------------------------
        public void Stop()
        {
            Log("SoundManager: Stop");
            StopSE();
            StopBGM(0.05f);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// BGMを再生中か？
        /// </summary>
        /// <returns>再生中ならtrue</returns>
        /// ----------------------------------------------------------------------
        public bool IsPlayBGM()
        {
            return _audioBGM.IsPlaying();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定BGMを再生中か？
        /// </summary>
        /// <returns>再生中ならtrue</returns>
        /// ----------------------------------------------------------------------
        public bool IsPlayBGM(string path)
        {
            return _audioBGM.IsPlaying(GAME_BGM_PATH + path);
        }

        /// <summary>
        /// フェード中か
        /// </summary>
        /// <returns></returns>
        public bool IsFadeBGM()
        {
            return _audioBGM.IsFade();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// SEを再生中か？
        /// </summary>
        /// <returns>再生中ならtrue</returns>
        /// ----------------------------------------------------------------------
        public bool IsPlaySE()
        {
            return _audioSE.IsPlaying();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定SEを再生中か？
        /// </summary>
        /// <returns>再生中ならtrue</returns>
        /// ----------------------------------------------------------------------
        public bool IsPlaySE(string path)
        {
            return _audioSE.IsPlaying(GAME_SE_PATH + path);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 現在再生中のBGMファイル名を取得
        /// </summary>
        /// <returns></returns>
        /// ----------------------------------------------------------------------
        public string[] GetPlayAudioNames()
        {
            List<string> nameList = _audioBGM.GetPlayAudioNames();
            return nameList.ToArray();
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// BGMの再生開始
        /// </summary>
        /// <remarks>
        /// BGMはストリームのみ対応。
        /// 再生時に読み込み、停止時に破棄する。
        /// というように、AudioClipのキャッシュは一切行わない。
        /// </remarks>
        /// <param name="path">再生ファイル名</param>
        /// <param name="fadeinTime">フェードイン時間</param>
        /// <returns></returns>
        /// ----------------------------------------------------------------------
        public async UniTask PlayBGM(string path, float fadeinTime = 0.0f, Action<string> errorCB = null)
        {
            var p = GAME_BGM_PATH + path;

            // AudioBgmManager.Play() 自体が、同一ファイルの複数再生抑制を
            // 行っているが、エラー処理のためこちらでもチェック
            if (_audioBGM.IsPlaying(p))
            {
                LogW($"SoundManager.Play: プレイ中に再生指定された [{p}]");
                return;
            }

            AudioHandler h = await _audioBGM.Play(p);
            if (h == null)
            {
                errorCB?.Invoke($"BGM再生失敗: {p}");
                return;
            }
            h.player.FadeIn(fadeinTime);
        }

        /// <summary>
        /// enumでBGM再生
        /// </summary>
        /// <param name="bgm"></param>
        /// <param name="fadeinTime"></param>
        /// <param name="errorCB"></param>
        /// <returns></returns>
        public async UniTask PlayBGM(Sound.BGM bgm, float fadeinTime = 0.0f, Action<string> errorCB = null)
        {
            await PlayBGM(Sound.GetBGM(bgm), fadeinTime, errorCB);
        }

        /// <summary>
        /// 既存のBGMを止めつつ新しいBGMを再生
        /// </summary>
        /// <param name="bgm"></param>
        /// <param name="fadeOut"></param>
        /// <param name="fadeIn"></param>
        /// <param name="errorCB"></param>
        /// <returns></returns>
        public async UniTask ChangeBGM(Sound.BGM bgm, float fadeOut = 0.0f, float fadeIn = 0.0f, Action<string> errorCB = null)
        {
            var path = Sound.GetBGM(bgm);
            // 同じBGMが再生中なら抜ける
            if (IsPlayBGM(path))
                return;
            if (IsPlayBGM())
            {
                // 再生中のを停止
                StopBGM(fadeOut);
                // 次のBGM開始がfadeInありの場合は、終わりを待たずにクロスフェードにする
                if (fadeIn == 0.0f)
                {
                    await UniTask.WaitUntil(() => !IsPlayBGM()); // 終了待ち
                }
            }
            await PlayBGM(path, fadeIn);
        }

        /// <summary>
        /// ジングルを再生
        /// </summary>
        /// <param name="jingle"></param>
        /// <returns></returns>
        public void PlayJingle(Sound.JINGLE jingle)
        {
            var path = Sound.GetJingle(jingle);
            PlaySE(path);
        }

        /// <summary>
        /// ジングルが再生中かどうか返す
        /// </summary>
        /// <param name="jingle"></param>
        /// <returns></returns>
        public bool IsPlayJingle(Sound.JINGLE jingle)
        {
            return IsPlaySE(Sound.GetJingle(jingle));
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 再生中のBGMを全て停止
        /// </summary>
        /// <param name="fadeoutTime">フェードアウト時間</param>
        /// ----------------------------------------------------------------------
        public void StopBGM(float fadeoutTime)
        {
            _audioBGM.FadeOut(fadeoutTime);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定ファイル名のBGMを停止
        /// </summary>
        /// <param name="path">ファイル名</param>
        /// <param name="fadeoutTime">フェードアウト時間</param>
        /// ----------------------------------------------------------------------
        public void StopBGM(string path, float fadeoutTime)
        {
            var p = GAME_BGM_PATH + path;
            if (!_audioBGM.IsPlaying(p))
            {
                LogW($"SoundManager.Stop: 再生していない曲を停止させようとした [{p}]");
                return;
            }
            _audioBGM.FadeOut(p, fadeoutTime);
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        /// <param name="pause">trueで一時停止</param>
        public void PauseBGM(bool pause)
        {
            _audioBGM.Pause(pause);
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        /// <param name="pause">trueで一時停止</param>
        /// <param name="fadeTime"></param>
        public void PauseBGM(bool pause, float fadeTime = 0f)
        {
            if (!_audioBGM.IsPlaying()) { return; }
            if (pause)
            {
                _audioBGM.FadeOut(fadeTime, () => { _audioBGM.Pause(pause); }, false);
            }
            else
            {
                _audioBGM.Pause(pause);
                _audioBGM.FadeIn(fadeTime);
            }
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pause">trueで一時停止</param>
        /// <param name="fadeTime"></param>
        public void PauseBGM(string path, bool pause, float fadeTime)
        {
            if (!_audioBGM.IsPlaying(path)) { return; }
            if (pause)
            {
                _audioBGM.FadeOut(fadeTime, () => { _audioBGM.Pause(path, pause); }, false);
            }
            else
            {
                _audioBGM.Pause(path, pause);
                _audioBGM.FadeIn(fadeTime);
            }
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定ファイル名のSEを再生
        /// </summary>
        /// <param name="path">SEファイル名</param>
        /// <returns>SEが存在しなければfalse</returns>
        /// ----------------------------------------------------------------------
        public bool PlaySE(string path, bool isLoop = false)
        {
            var p = GAME_SE_PATH + path;
            // SoundManager経由でのSE再生は全て事前キャッシュ済みの想定
            if (!_audioSE.IsCached(p))
            {
                LogW($"SE素材： {p} が読み込めませんでした");
                return false;
            }
            _ = _audioSE.Play(p, isLoop);
            return true;
        }

        public bool PlaySE(Sound.SE se, bool isLoop = false)
        {
            return PlaySE(Sound.GetSE(se), isLoop);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定ファイル名のSEを再生（再生制御用ハンドラを返す）
        /// </summary>
        /// <param name="se">SE定義値</param>
        /// <param name="isLoop">ループ再生ならtrue</param>
        /// <returns>再生したSEのハンドラ</returns>
        /// <remarks>
        /// 再生中のSEの制御を行いたい場合は、AudioHanlerを使用して個別制御処理を実行する。
        /// AudioHandler を保持しておけば、AudioBaseにあるハンドラ指定の各種制御関数を使用可能。
        /// 例：
        /// 例えばフェードインで再生
        /// _handler = await SoundManager.PlaySEControllable(Sound.SE.SE101, true);
        /// SoundManager.Instance.AudioSE.FadeIn(handler, 1.0f);
        /// その後、フェードでボリュームを下げる
        /// SoundManager.Instance.AudioSE.Fade(handler, 1.0f, 0.5f);
        /// 
        /// </remarks>
        /// ----------------------------------------------------------------------
        public async UniTask<AudioHandler> PlaySEControllable(Sound.SE se, bool isLoop = false)
        {
            var path = Sound.GetSE(se);
            AudioHandler handler = await _audioSE.Play(path, isLoop);
            return handler;
        }

        [Obsolete("全SEを停止するので非推奨。未使用が確認され次第削除")]
        public bool isStopPlaySE(Sound.SE se, bool isStop = false, bool isLoop = false)
        {
            // 他のSEを止めるか
            if (isStop)
            {
                // 他のSEが鳴っているなら、停止してから再生する
                if (IsPlaySE())
                {
                    _audioSE.Stop();
                }
            }

            return PlaySE(Sound.GetSE(se), isLoop);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// 指定ファイル名のSEを再生（多重登録は行わない）
        /// </summary>
        /// <remarks>
        /// 同一SEが既になっているなら、停止した後改めて再生する。
        /// </remarks>
        /// <param name="path">SEファイル名</param>
        /// <returns>SEが存在しなければfalse</returns>
        /// ----------------------------------------------------------------------
        public bool StopAndPlaySE(string path)
        {
            if (IsPlaySE(path))
            {
                StopSE(path);
            }
            return PlaySE(path);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// SEを停止
        /// </summary>
        /// <param name="path"></param>
        /// ----------------------------------------------------------------------
        public void StopSE(string path = null)
        {
            if (path == null)
            {
                _audioSE.Stop();
            }
            else
            {
                _audioSE.Stop(GAME_SE_PATH + path);
            }
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// SEをフェードアウト停止
        /// </summary>
        /// <param name="path"></param>
        /// ----------------------------------------------------------------------
        public void StopFadeSE(float fadeoutTime)
        {
            _audioSE.FadeOut(fadeoutTime);
        }

        /// ----------------------------------------------------------------------
        /// <summary>
        /// ファイル名からAudioTypeを取得
        /// </summary>
        /// <param name="path">ファイル名</param>
        /// <returns>AudioType</returns>
        /// ----------------------------------------------------------------------
        public AudioType GetAudioType(string path)
        {
            string ext = System.IO.Path.GetExtension(path);
            if (ext == ".wav")
            {
                return AudioType.WAV;
            }
            else if (ext == ".mp3")
            {
                return AudioType.MPEG;
            }
            else if (ext == ".ogg")
            {
                return AudioType.OGGVORBIS;
            }
            else if (ext == ".aif")
            {
                return AudioType.AIFF;
            }
            return AudioType.UNKNOWN;
        }


        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Log(object obj) { if (LogEnable) { Debug.Log(obj); } }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void LogW(object obj) { Debug.LogWarning(obj); }
    }
}
