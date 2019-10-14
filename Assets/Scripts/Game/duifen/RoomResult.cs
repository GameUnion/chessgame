﻿using System;
using System.Collections.Generic;
using Assets.Scripts.Game.duifen.ImgPress;
using Assets.Scripts.Game.duifen.ImgPress.Main;
using com.yxixia.utile.YxDebug;
using Sfs2X.Entities.Data;
using UnityEngine;
using YxFramwork.Common;
using YxFramwork.Controller;
using YxFramwork.Framework.Core;
using YxFramwork.Manager;
using YxFramwork.Tool;

namespace Assets.Scripts.Game.duifen
{
    [RequireComponent(typeof(CompressImg))]
    public class RoomResult : MonoBehaviour
    {
        [HideInInspector]
        public GameObject[] ResultItems;

        public GameObject Prefab;

        public CompressImg Img;

        public UIGrid Grid;

        // ReSharper disable once InconsistentNaming
        public UILabel RoomIDLabel;

        public UILabel Round;

        public UILabel Time;

        
        public void ShowResultView(ISFSObject data)
        {
            if(!data.ContainsKey("users"))
                return;

            if(data.ContainsKey("id") && data.GetInt("id") < 0)
            {
                return;
            }

            ISFSArray userArray = data.GetSFSArray("users");

            int count = userArray.Count;
            InitResultItemArray(count);     //初始化ResultItems

            int bigWinnerIndex = -1;        //大赢家的索引记录
            int highScore = 0;              //大赢家的得分记录

            var roomInfo = App.GetGameManager<DuifenGameManager>().RoomInfo;
            RoomIDLabel.text = roomInfo.RoomID.ToString();
            Round.text = roomInfo.CurrentRound + "/" + roomInfo.MaxRound;
            Time.text = ToRealTime(data.GetLong("svt"));

            //初始化所有玩家信息
            for (int i = 0; i < ResultItems.Length; i++)
            {
                //初始化结算界面
                if (ResultItems[i] == null)
                {
                    GameObject item = Instantiate(Prefab);
                    item.transform.parent = Prefab.transform.parent;
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localScale = Vector3.one;
                    ResultItems[i] = item;
                }

                if (i >= count)
                {
                    ResultItems[i].SetActive(false);
                    continue;
                }

                //设置玩家信息
                ISFSObject itemData = userArray.GetSFSObject(i);
                
                ResultItems[i].GetComponent<ResultItemInfo>().InitItem(itemData);

                //获得大赢家信息
                int score = itemData.GetInt("gold");
                if (score > highScore)
                {
                    bigWinnerIndex = i;
                    highScore = score;
                }
            }

            YxDebug.Log(" ==== bigWinnerIndex == " + bigWinnerIndex + " ==== ");
            if(bigWinnerIndex >= 0)
            {
                ResultItems[bigWinnerIndex].GetComponent<ResultItemInfo>().SetBigWinnerMark(true);      //设置大赢家信息
            }

            gameObject.SetActive(true);
            Grid.Reposition();
        }


        /// <summary>
        /// 时间戳的起始时间
        /// </summary>
        DateTime _dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

        /// <summary>
        /// 将时间戳转换为正常时间
        /// </summary>
        /// <param name="timpStamp">服务器传输的时间数据</param>
        /// <returns></returns>
        string ToRealTime(long timpStamp)
        {
            long unixTime = timpStamp * 10000000L;
            TimeSpan toNow = new TimeSpan(unixTime);
            DateTime dt = _dtStart.Add(toNow);
            return dt.ToString("yyyy/MM/dd     HH:mm:ss");
        }


        private void InitResultItemArray(int count)
        {
            if (ResultItems == null)
            {
                ResultItems = new GameObject[count];
                return;
            }

            if (ResultItems.Length < count)
            {
                GameObject[] tempArray = new GameObject[count];
                Array.Copy(ResultItems, tempArray, ResultItems.Length);
                ResultItems = tempArray;
            }
        }


        /// <summary>
        /// 点击返回大厅按钮
        /// </summary>
        public void OnClickBackBtn()
        {
            YxDebug.Log(" === 返回大厅 === ");
            App.QuitGame();
          
        }


        /// <summary>
        /// 点击分享按钮
        /// </summary>
        public void OnClickShare()
        {
            if (Img == null)
                Img = gameObject.GetComponent<CompressImg>();

            YxWindowManager.ShowWaitFor();

            Facade.Instance<WeChatApi>().InitWechat();

            UserController.Instance.GetShareInfo(delegate (ShareInfo info)
            {
                YxWindowManager.HideWaitFor();
                Img.DoScreenShot(new Rect(0, 0, Screen.width, Screen.height), imageUrl =>
                {
                    YxDebug.Log("Url == " + imageUrl);
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        imageUrl = "file://" + imageUrl;
                    }
                    info.ImageUrl = imageUrl;
                    info.ShareType = ShareType.Image;
                    Facade.Instance<WeChatApi>().ShareContent(info, str =>
                    {
                        //成功后给奇哥发消息
                        var parm = new Dictionary<string, object>
                                        {
                                            {"option", 2},
                                            {"bundle_id", Application.bundleIdentifier},
                                            {"share_plat", SharePlat.WxSenceTimeLine.ToString()},
                                        };
                        Facade.Instance<TwManager>().SendAction("shareAwards", parm, null);
                    });
                });
            });
            //YxDebug.Log(" === 点击分享按钮 === ");
            //YxWindowManager.ShowWaitFor();

            //Facade.Instance<WeChatApi>().InitWechat(AppInfo.AppId);

            //UserController.Instance.GetShareInfo(info =>
            //{
            //    YxWindowManager.HideWaitFor();
            //    Img.DoScreenShot(new Rect(0, 0, Screen.width, Screen.height), imageUrl =>
            //    {
            //        if (Application.platform == RuntimePlatform.Android)
            //        {
            //            imageUrl = "file://" + imageUrl;
            //        }
            //        info.ImageUrl = imageUrl;
            //        info.ShareType = ShareType.Image;
            //        Facade.Instance<WeChatApi>().ShareContent(info, str =>
            //        {
            //            Dictionary<string, object> parm = new Dictionary<string, object>()
            //            {
            //                {"option",2},
            //                {"bundle_id",Application.bundleIdentifier},
            //                {"share_plat",SharePlat.WxSenceTimeLine.ToString() },
            //            };
            //            Facade.Instance<TwManger>().SendAction("shareAwards", parm, null);
            //        });
            //    });
            //});
        }
    }
}