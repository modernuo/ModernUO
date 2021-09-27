using Server.Gumps;
using Server.Network;
using System;
using System.IO;
using System.Text;

namespace Server.Gumps
{
    public class SciterTestGump : Gump
    {
        public class Shop
        {
            public string[] ShopItems { get; set; } = new string[] { "item1", "item2" };
        }
       
        public SciterTestGump(Mobile user) : base(
            0,
            0
        )
        {
            UseWebRender = true;
            AddBody(Body());
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;
            switch (info.ButtonID)
            {
                case 0: // close
                    sender.RemoveGump(this);
                    break;
                case 1: // hide
                    from.Hidden = true;
                    break;
                case 2: // unhide
                    from.Hidden = false;
                    break;
                case 3: // sendDataToCLeint
                    SendData(from, "ShowSend", new Shop());
                    break;
                case 4: // showData
                    var builder = new StringBuilder();
                    for (int i = 0; i < info.TextEntries.Length; i++)
                    {
                        var test = info.TextEntries[i].Text;
                        builder.Append("argument ");
                        builder.Append(i.ToString());
                        builder.Append(" = ");
                        builder.Append(test);
                        builder.Append(" ");
                    }

                    from.SendAsciiMessage(builder.ToString());

                    break;
                default:
                    break;


            }
        }
        public string Body()
        {
            return "<html>\r\n" +
   "    <head>\r\n" +
   "        <title>Test<" +
   "/title>\r\n" +
   "        <style>\r\n" +
   "          [onmousele" +
   "ave] { aspect: \"Emu" +
   ".onmouseleave\"; }\r" +
   "\n" +
   "            html {\r" +
   "\n" +
   "                over" +
   "flow: hidden;\r\n" +
   "                \r\n" +
   "            }\r\n" +
   "  \r\n" +
   " .btn {\r\n" +
   "  border: 2px solid " +
   "black;\r\n" +
   "  border-radius: 5px" +
   ";\r\n" +
   "  background-color: " +
   "white;\r\n" +
   "  color: black;\r\n" +
   "  padding: 14px 28px" +
   ";\r\n" +
   "  font-size: 16px;\r" +
   "\n" +
   "  cursor: pointer;\r" +
   "\n" +
   "}        \r\n" +
   ".success {\r\n" +
   "  border-color: #04A" +
   "A6D;\r\n" +
   "  color: green;\r\n" +
   "}\r\n\r\n" +
   ".success:hover {\r\n" +
   "  background-color: " +
   "#04AA6D;\r\n" +
   "  color: white;\r\n" +
   "}\r\n\r\n" +
   "/* Blue */\r\n" +
   ".info {\r\n" +
   "  border-color: #219" +
   "6F3;\r\n" +
   "  color: dodgerblue" +
   "\r\n" +
   "}\r\n\r\n" +
   ".info:hover {\r\n" +
   "  background: #2196F" +
   "3;\r\n" +
   "  color: white;\r\n" +
   "}\r\n\r\n" +
   "/* Orange */\r\n" +
   ".warning {\r\n" +
   "  border-color: #ff9" +
   "800;\r\n" +
   "  color: orange;\r\n" +
   "}\r\n\r\n" +
   ".warning:hover {\r\n" +
   "  background: #ff980" +
   "0;\r\n" +
   "  color: white;\r\n" +
   "}\r\n\r\n" +
   "/* Red */\r\n" +
   ".danger {\r\n" +
   "  border-color: #f44" +
   "336;\r\n" +
   "  color: red\r\n" +
   "}\r\n\r\n" +
   ".danger:hover {\r\n" +
   "  background: #f4433" +
   "6;\r\n" +
   "  color: white;\r\n" +
   "}\r\n\r\n" +
   "/* Gray */\r\n" +
   ".default {\r\n" +
   "  border-color: #e7e" +
   "7e7;\r\n" +
   "  color: black;\r\n" +
   "}\r\n\r\n" +
   ".default:hover {\r\n" +
   "  background: #e7e7e" +
   "7;\r\n" +
   "}\r\n\r\n" +
   "        </style>\r\n" +
   "    <script type=\"t" +
   "ext/tiscript\">\r\n" +
   "\r\n" +
   "        namespace Em" +
   "u {\r\n" +
   "          function o" +
   "nmouseleave() {\r\n" +
   "              this.o" +
   "n(\"mouseleave\", fu" +
   "nction(evt) {\r\n" +
   "                \r\n" +
   "              });\r" +
   "\n" +
   "          }\r\n" +
   "        }\r\n\r\n" +
   "        function sel" +
   "f.ready() {\r\n" +
   "          view.move(" +
   "400px,100px,500,300)" +
   ";\r\n" +
   "          view.windo" +
   "wTopmost = false;\r" +
   "\n" +
   "          //focus on" +
   " form \r\n" +
   "          //this.tim" +
   "er(20ms, function() " +
   "{ view.focusable(#fi" +
   "rst).state.focus = t" +
   "rue; });\r\n" +
   "        }\r\n\r\n" +
   "        self.on(\"ke" +
   "ydown\", function(ev" +
   "t) {\r\n" +
   "           if(evt.ke" +
   "yCode == 27)\r\n" +
   "           {\r\n" +
   "              close(" +
   ");\r\n" +
   "           }\r\n" +
   "        });\r\n\r\n" +
   "        function Sho" +
   "wSend(data)\r\n" +
   "        {\r\n" +
   "          var input " +
   "= $(#test);\r\n" +
   "          if(element" +
   ")return;  \r\n" +
   "          input.clea" +
   "r();\r\n\r\n" +
   "          for (var i" +
   " = 0; i < data.ShopI" +
   "tems.length; i++) {" +
   "\r\n" +
   "            input.ap" +
   "pend(data.ShopItems[" +
   "i]+\' ,\');\r\n" +
   "          }\r\n" +
   "        }\r\n\r\n" +
   "        function clo" +
   "se()\r\n" +
   "        {\r\n" +
   "          view.Host_" +
   "SEND(0);\r\n" +
   "          view.windo" +
   "wState = View.WINDOW" +
   "_HIDDEN;\r\n" +
   "          view.move(" +
   "0px,0px,0,0);\r\n" +
   "        }\r\n" +
   "       \r\n\r\n\r\n" +
   "        event click " +
   "$(button#load) {\r\n" +
   "            view.Hos" +
   "t_SEND(3);\r\n" +
   "        }\r\n\r\n" +
   "        event click " +
   "$(button#send) {\r\n" +
   "            view.Hos" +
   "t_SEND(4,\"arg0\",\"" +
   "arg1\",\"arg2\",\"ar" +
   "g3\",\"arg4\",\"arg5" +
   "\",\"arg6\",\"arg7\"" +
   ",\"arg8\",\"arg9\");" +
   "\r\n" +
   "        }\r\n\r\n" +
   "        event click " +
   "$(button#hide) {\r\n" +
   "            view.Hos" +
   "t_SEND(1,\"HELLO WOR" +
   "LD YEAH\");\r\n" +
   "        }\r\n\r\n" +
   "        event click " +
   "$(button#unhide) {\r" +
   "\n" +
   "            view.Hos" +
   "t_SEND(2);\r\n" +
   "        }\r\n\r\n" +
   "        event click " +
   "$(button#close) {\r" +
   "\n" +
   "          close();\r" +
   "\n" +
   "        }\r\n" +
   "        \r\n\r\n" +
   "    </script>\r\n" +
   "    \r\n" +
   "    </head>\r\n" +
   "    <body onmouselea" +
   "ve>\r\n" +
   "        <button#load" +
   " class=\"btn warning" +
   "\" onmouseenter onmo" +
   "useleave>getDataFrom" +
   "Server</button>\r\n" +
   "        <button#send" +
   " class=\"btn warning" +
   "\" onmouseenter onmo" +
   "useleave>sendDataToS" +
   "erver</button>\r\n" +
   "        <button#hide" +
   " class=\"btn warning" +
   "\" onmouseenter onmo" +
   "useleave>hide</butto" +
   "n>\r\n" +
   "        <button#unhi" +
   "de class=\"btn info" +
   "\">unhide</button>\r" +
   "\n" +
   "        <button#clos" +
   "e class=\"btn danger" +
   "\">Close</button>\r" +
   "\n" +
   "        <textarea id" +
   "=\"test\" name=\"com" +
   "ment\"></textarea></" +
   "p>\r\n" +
   "    </body>\r\n" +
   "</html>";
        }
    }
}
