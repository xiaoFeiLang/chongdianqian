using Microsoft.ApplicationBlocks.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common;
using System.Xml;

namespace DoorAuthority
{
    /// <summary>
    /// DoorAuthority 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class DoorAuthority : System.Web.Services.WebService
    {
        [WebMethod]
        public string HelloWorld()
        {
            string path = Server.MapPath("~/Door.xml");
            //<1>实例化一个XML文档操作对象.
            XmlDocument doc = new XmlDocument();

            //<2>使用XML对象加载XML.
            doc.Load(path);
            //通过string方式加载
           // doc.LoadXml(xmlstr); //xml格式的字符串
                                 //<3>获取根节点.
            //XmlNode root = doc.SelectSingleNode("ArrayOfAuthority");
            ////或者通过以下方式获得
            ////XmlNode root = doc.FirstChild;
            ////<4>获取根节点下所有子节点.
            //XmlNodeList nodeList = root.ChildNodes;
            XmlNodeList topM = doc.SelectNodes("//Authority");

            foreach (XmlElement element in topM)
            {
                string id = element.GetElementsByTagName("ID")[0].InnerText;
                string domainName = element.GetElementsByTagName("DomainName")[0].InnerText;
                 
            }
            //<5>遍历输出.
            //foreach (XmlNode node in nodeList)
            //{
            //    //取属性.
            //    int id = int.Parse(node.Attributes["id"].Value);
            //    //取文本.
            //    string name = node.ChildNodes[0].InnerText;
            //    string url = node.ChildNodes[1].InnerText;

                 
            //}
            
            List<Authority> list = new List<Authority>();
            for (int i = 0; i < 100; i++) {

                list.Add(new Authority() { Code = i.ToString(), Name = "laic" });
            }

            string xmlStr = XmlUtility.SerializeToXml<List<Authority>>(list);
            return xmlStr;
        }
        [WebMethod]
        public string Door_Authority(string jsonText)
        {
            string sqlResult = "";  //执行存储过程后返回值
            int level = 0;
            string result = "";
            string doorType = "";  // D 部门门禁  F 人脸  S特殊门禁
            JArray resultArray = (JArray)JsonConvert.DeserializeObject(jsonText);
            //["0","",[{"empid":"0062815","doorname":"201-L40-DOOR06"}]]
            //["1","权限5-2质量管理部(PQA)",[{"empid":"0062815","doorname":""},{"empid":"0060150","doorname":""}]]
            //["0","大门门禁",[{"empid":"0062815","doorname":"门岗-ACS01A-DOOR01"}]]
            //["1","3",[{"empid":"0062815","doorname":""}]]
            string doortype = resultArray[0].ToString();  // 0  特殊  1  部门
            JArray data = (JArray)resultArray[2];  //详细表

            int levelName = (int)resultArray[1];  //获取部门权限级别名称
            string path = Server.MapPath("~/Door.xml");
            //<1>实例化一个XML文档操作对象.
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList topM = doc.SelectNodes("//Authority");
            string levelname = "";
            foreach (XmlElement element in topM)
            {
                string id = element.GetElementsByTagName("Code")[0].InnerText;
                if (id == levelName.ToString())
                {
                    levelname = element.GetElementsByTagName("Name")[0].InnerText;
                    break;
                }
            }

            if (doortype == "0")  //特殊门禁
            {
                for(int i=0; i<data.Count; i++)
                {

                    JObject jodata = (JObject)data[i];  
                    string doorName = jodata["doorname"].ToString();
                    if (doorName.Contains("101"))
                        doorName = doorName.Replace("101", "FAB");
                    if (doorName.Contains("201"))
                        doorName = doorName.Replace("201", "OFFICE");
                    if (doorName.Contains("电梯"))
                        doorName = "";
                    string insertStr = "%";
                    string isCS = jodata.ContainsKey("sfwcs")? jodata["sfwcs"].ToString() : "";
                    if(isCS == "0" && isCS != "")  //为厂商 新增资料到hr_emp
                    {
                        string csName = jodata["xm"].ToString();
                        string sql_InsertCS = string.Format(@"declare @door_result varchar(10)
                                                Exec [Door_InsertCS] '{0}',@door_result out
                                                select @door_result ", csName);
                        DataTable dt_check = new MarkCoeno().ExecuteQuery(sql_InsertCS).Tables[0];
                        sqlResult = Int32.Parse(dt_check.Rows[0][0].ToString()).ToString();
                        if(sqlResult == "1")
                        {
                            result = "0";
                        }
                        else
                        {
                            result = "厂商资料写入数据库失败，开单失败";
                        }
                        return result;
                    }
                    else if (isCS == "1" && isCS != "")
                    {
                        // 找到最后一个 -的位置，插入字符
                        doorName = doorName.Insert(doorName.LastIndexOf('-'), insertStr);
                        string empid = jodata["empid"].ToString();
                        doorType = "S"; // special 特殊门禁
                                        //20190603 新增逻辑：如果该门属于所选部门级别 则起单  否则 不往下起单
                        string sql_checkAuthority = string.Format(@"declare @door_result varchar(10)
                                                                Exec [Door_CheckAuthority] '{0}','{1}',@door_result out
                                                                select @door_result ", doorName, levelname);
                        DataTable dt_check = new MarkCoeno().ExecuteQuery(sql_checkAuthority).Tables[0];
                        sqlResult = Int32.Parse(dt_check.Rows[0][0].ToString()).ToString();
                        if (sqlResult == "1")
                        {
                            string sql_openAuthority = string.Format(@"declare @return_value varchar(2) EXEC Door_OpenAuthority '{0}','{1}','{2}','{3}', @return_value output 
                                                         SELECT @return_value ", empid, level, doorType, doorName);
                            //存储过程逻辑： 根据传入工号获取卡内码 转码 查找该卡内码是否存在级别，没有则添加级别 再加门 
                            //加门逻辑： 拿到级别号 根据特殊门字符串拿到编号 添加
                            DataTable dt_open = new MarkCoeno().ExecuteQuery(sql_openAuthority).Tables[0];
                            sqlResult = Int32.Parse(dt_open.Rows[0][0].ToString()).ToString();
                            if (sqlResult == "0")  //成功
                            {
                                result = "0";
                            }
                            else  //失败
                            {
                                result = result + empid + ",";
                            }
                        }
                        else
                        {
                            result = "该级别下没有查到此特殊门，开单失败";
                        }
                    }
                    
                }
            }
            else if (doortype == "1")  //部门门禁
            {
                
                // string levelName = jodata["levelname"].ToString();
                string sql_getLevel = string.Format(@"declare @return_value int EXEC Door_GetLevel '{0}',@return_value output 
                                                    SELECT @return_value", levelname);
                DataTable dt = new MarkCoeno().ExecuteQuery(sql_getLevel).Tables[0];
                level = Int32.Parse(dt.Rows[0][0].ToString());  //门禁级别
                if (level == 3569) //人脸识别-ACF管制口
                { 
                    level = 6;  //重新赋值  人脸系统的groupid
                    doorType = "F"; //Face  人脸
                }
                else if (level == 3794) // 人脸识别-CELL管制口
                {
                    level = 5;
                    doorType = "F";
                }
                else if (level == 3804) // 人脸识别-实验室DOOR100
                {
                    level = 11;
                    doorType = "F";
                }
                else if (level == 3816) // 人脸识别-实验室DOOR46
                {
                    level = 7;
                    doorType = "F";
                }
                else if (level == 3803) // 人脸识别-实验室DOOR53
                {
                    level = 8;
                    doorType = "F";
                }
                else if (level == 3802) // 人脸识别-实验室DOOR58
                {
                    level = 9;
                    doorType = "F";
                }
                else if (level == 3800) // 人脸识别-实验室DOOR63
                {
                    level = 10;
                    doorType = "F";
                }
                else  //门禁系统
                {
                    doorType = "D";  //Door 普通门禁
                }
                for (int i=0; i<data.Count; i++)
                {
                    try
                    {
                        JObject jodata = (JObject)data[i];
                        string isCS = jodata["sfwcs"].ToString();
                        if (isCS == "0")  //为厂商 新增资料到hr_emp
                        {
                            string csName = jodata["xm"].ToString();
                            string sql_InsertCS = string.Format(@"declare @door_result varchar(10)
                                                Exec [Door_InsertCS] '{0}',@door_result out
                                                select @door_result ", csName);
                            DataTable dt_check = new MarkCoeno().ExecuteQuery(sql_InsertCS).Tables[0];
                            sqlResult = Int32.Parse(dt_check.Rows[0][0].ToString()).ToString();
                            if (sqlResult == "1")
                            {
                                result = "0";
                            }
                            else
                            {
                                result = "厂商资料写入数据库失败，开单失败";
                            }
                            return result;
                        }
                        else
                        {
                            string empid = jodata["empid"].ToString();
                            string sql_openAuthority = string.Format(@"declare @return_value varchar(2) EXEC Door_OpenAuthority '{0}','{1}','{2}','{3}', @return_value output 
                                                         SELECT @return_value ", empid, level, doorType, "");
                            DataTable dt_open = new MarkCoeno().ExecuteQuery(sql_openAuthority).Tables[0];
                            sqlResult = Int32.Parse(dt_open.Rows[0][0].ToString()).ToString();
                            if (sqlResult == "1")
                            {
                                result = result + empid + ",";
                            }
                            else if (sqlResult == "")
                            {
                            }
                        }
                        
                        
                    }catch(Exception ex)
                    {
                        result = ex.Message;
                    }
                }
                if (result != "")
                    result = result.TrimEnd(',');
                else
                    result = "0";
            }
            return result;
        }

        [WebMethod]
        public string GetEmpInfo()
        {
            string result = "";
            string empid = "0062815";
            //对empid验证  goto
            string sql_openAuthority = string.Format(@"select a.empid,a.empname,b.deptname from hr_emp a 
                                                     join hr_dept b on a.deptid=b.deptid where a.empid='"+ empid + "' and a.staid=0");  // 
            DataTable dt = new MarkCoeno().ExecuteQuery(sql_openAuthority).Tables[0];
            if(dt.Rows.Count>0)
            {
                string empid1 = dt.Rows[0]["empid"].ToString();
                string empname = dt.Rows[0]["empname"].ToString();
                string deptname = dt.Rows[0]["deptname"].ToString();
                result = empid1 + "-" + empname + "-" + deptname;
            }
            else
            {
                result = "没有查到相关信息";
            }
            return result;
        }

        /// <summary>
        /// 写日志  点击查询写日志 查询内容 时间 操作人 充值写入log  充值结果写入log
        /// </summary>
        [WebMethod]
        public void WriteLog()
        {

        }
        /// <summary>
        /// 充值成功后调用显示
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public string GetBalance()
        {
            string result = "";
            string sql_openAuthority = string.Format(@"select a.totalje+a.lastmonthye+a.addje-a.decje-a.useje as btye,
                                                    c.totalje-c.useje as czye from xf_bt a
                                                    full join
                                                    xf_cz c on a.empid=c.empid where a.empid='0062815' and a.strmonth='2019-06'");
            DataTable dt = new MarkCoeno().ExecuteQuery(sql_openAuthority).Tables[0];
            if (dt.Rows.Count > 0)
            {
                string btye = dt.Rows[0]["btye"].ToString();
                string czye = dt.Rows[0]["czye"].ToString();
                result = btye + "-" + czye;
            }
            else
            {
                result = "没有查到相关信息";
            }
            return result;
        }
    }
}
