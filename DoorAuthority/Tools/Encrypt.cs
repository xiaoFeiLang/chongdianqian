using System;
using System.Text;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;
using System.Security.Cryptography;

namespace  MarkClass 
{
/// <summary>
/// Encrypt 的摘要描述
/// </summary>
    public class Encrypts
    {
	    //
	    // TODO: 在此加入建構函式的程式碼
	    //

         static Encrypts()
        {
            Initialize();
        }
        public static void Initialize()
        {
           
        }

    

        #region C#對稱算法一
        /// <summary>
        /// DecryptString
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string EncryptString(string v_EnValue, string v_Key, string v_InitVI)
        {
          ICryptoTransform ct;
          MemoryStream ms;
          CryptoStream cs;
          byte[] byt;
          SymmetricAlgorithm mCSP;

          //轉化密鑰
          mCSP = new DESCryptoServiceProvider();
          //byte[] byt1 = Convert.FromBase64String(v_Key);
          //byte[] byt1 = System.Text.Encoding.ASCII.GetBytes(v_Key);
          byte[] byt1 = Encoding.UTF8.GetBytes(v_Key);
          mCSP.Key = byt1;

          //轉化初使化向量
          //byte[] byt2 = Convert.FromBase64String(v_InitVI);
          //byte[] byt2 = System.Text.Encoding.ASCII.GetBytes(v_InitVI);
          byte[] byt2 = Encoding.UTF8.GetBytes(v_InitVI);
          mCSP.IV = byt2;


          ct = mCSP.CreateEncryptor(mCSP.Key, mCSP.IV);


          byt = Encoding.UTF8.GetBytes(v_EnValue);

          ms = new MemoryStream();
          cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
          cs.Write(byt, 0, byt.Length);
          cs.FlushFinalBlock();

          cs.Close();

          return Convert.ToBase64String(ms.ToArray());
          //return System.Text.Encoding.ASCII.GetString(ms.ToArray());

        }

        /// <summary>
        /// DecryptString
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string DecryptString(string v_DeValue, string v_Key, string v_InitVI)
        {
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt = new byte[v_DeValue.Length];

            SymmetricAlgorithm mCSP;


            //轉化密鑰
            mCSP = new DESCryptoServiceProvider();
            //byte[] byt1 = Convert.FromBase64String(v_Key);
            //byte[] byt1 = System.Text.Encoding.ASCII.GetBytes(v_Key);
            byte[] byt1 = Encoding.UTF8.GetBytes(v_Key);
            mCSP.Key = byt1;

            //轉化初使化向量
            //byte[] byt2 = Convert.FromBase64String(v_InitVI);
            //byte[] byt2 = System.Text.Encoding.ASCII.GetBytes(v_InitVI);
            byte[] byt2 = Encoding.UTF8.GetBytes(v_InitVI);
            mCSP.IV = byt2;

            ct = mCSP.CreateDecryptor(mCSP.Key, mCSP.IV);

            byt = Convert.FromBase64String(v_DeValue);
            //byt = System.Text.Encoding.ASCII.GetBytes(v_DeValue);

            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();  
            cs.Close();

            return Encoding.UTF8.GetString(ms.ToArray());

        }

        #endregion

        #region C#簡單加密算法二
        /// <summary>
        /// EncryptByASCII
        /// </summary>
        /// <param name="strSource"></param>
        /// <returns></returns>
        public static string EncryptByASCII(string strSource)
        {

            char[] charArray = strSource.ToCharArray();

            string strResult = "";

            Random rdmKey = new Random();

            int Key = rdmKey.Next(50);

            int iRemainder = Key % 26;

            strResult = strResult + ((char)(iRemainder + 65)).ToString();

            int iTimes = Key / 26;

            strResult = strResult + iTimes.ToString();

            foreach (char a in charArray)
            {

                int k = (int)a + Key;

                iRemainder = k % 26;

                strResult = strResult + ((char)(iRemainder + 65)).ToString();

                iTimes = k / 26;

                strResult = strResult + iTimes.ToString();

            }

            return strResult;

        }


        /// <summary>
        /// DecryptByASCII
        /// </summary>
        /// <param name="strSource"></param>
        /// <returns></returns>
        public static string DecryptByASCII(string strSource)
        {

            char[] charArray = strSource.ToCharArray();

            string strResult = "";

            int iRemainder = charArray[0] - 'A';

            int iTimes = charArray[1] - '0';

            int Key = 26 * iTimes + iRemainder;

            for (int i = 2; i < charArray.Length; i = i + 2)
            {

                iRemainder = charArray[i] - 'A';

                iTimes = charArray[i + 1] - '0';

                int a = 26 * iTimes + iRemainder - Key;

                strResult = strResult + ((char)a).ToString();

            }

            return strResult;

        }
        

    #endregion

 }
}

