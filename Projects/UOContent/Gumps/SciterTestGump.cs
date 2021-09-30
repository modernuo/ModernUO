using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.IO;
using System.Text;

namespace Server.Gumps
{
    public class SciterTestGump : Gump
    {
        public class Item
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
        public class Shop
        {
            public Item[] ShopItems { get; set; } = new Item[]
            {
                new Item(){Name="Arrow",Count = 10 },
                new Item(){Name="Bolt",Count = 10 },
                new Item(){Name="Gold",Count = 20000 },
            };
        }
        public Shop _shop = new Shop();
        public SciterTestGump(Mobile user) : base(
            0,
            0
        )
        {
            UseWebRender = true;
            AddBody(body());
        }


        public void TryToAddItem(Mobile m, int index)
        {
            if (_shop.ShopItems.Length < index)
                return;

            var itemName = _shop.ShopItems[index].Name;
            var itemCount = Convert.ToInt32(_shop.ShopItems[index].Count);

            var type = AssemblyHandler.FindTypeByName(itemName);

            var item = Activator.CreateInstance(type, args: new object[] { itemCount });
            if (item is Server.Item _item)
            {
                m.AddToBackpack(_item);
            }

        }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;
            switch (info.ButtonID)
            {
                case 0: // close
                    sender.RemoveGump(this);
                    break;
                case 1:
                    SendData(from, "1", _shop);
                    break;
                case 2:
                    TryToAddItem(from, Convert.ToInt32(info.TextEntries[0].Text));
                    break;

                default:
                    break;
            }

        }
        public string body()
        {
            return  "<html >\r\n" +
   "  \r\n" +
   "  <style>\r\n" +
   "      html{backgroun" +
   "d-color:black}\r\n" +
   "      h1 { color: wh" +
   "ite;}\r\n" +
   "      p{color: white" +
   ";}\r\n" +
   "      .btn1 {\r\n" +
   "            backgrou" +
   "nd-color: yellow;\r" +
   "\n" +
   "            color: b" +
   "lack;\r\n" +
   "            text-ali" +
   "gn: center;\r\n" +
   "            font-siz" +
   "e: 13px;\r\n" +
   "      }\r\n" +
   "      #close {\r\n" +
   "        position: re" +
   "lative;\r\n" +
   "        float: right" +
   ";\r\n" +
   "        \r\n" +
   "      }\r\n" +
   "            \r\n" +
   "      .center {\r\n" +
   "        position: re" +
   "lative;\r\n" +
   "        float: cente" +
   "r;\r\n" +
   "      }   \r\n" +
   " \r\n" +
   "      .close-btn {\r" +
   "\n" +
   "        font-size: 6" +
   "0px;\r\n" +
   "        font-weight:" +
   " bold;\r\n" +
   "        color: #000;" +
   "\r\n" +
   "      }\r\n" +
   "      .tsize\r\n" +
   "      {\r\n" +
   "            color: r" +
   "ed;\r\n" +
   "            font-siz" +
   "e: 18px;\r\n" +
   "      }\r\n" +
   "          .myButton " +
   "{\r\n" +
   "          box-shadow" +
   ":inset 0px 34px 0px " +
   "-15px #b54b3a;\r\n" +
   "          background" +
   ":linear-gradient(to " +
   "bottom, #a73f2d 5%, " +
   "#b34332 100%);\r\n" +
   "          background" +
   "-color:#a73f2d;\r\n" +
   "          border:1px" +
   " solid #241d13;\r\n" +
   "          display:in" +
   "line-block;\r\n" +
   "          cursor:poi" +
   "nter;\r\n" +
   "          color:#fff" +
   "fff;\r\n" +
   "          font-famil" +
   "y:Arial;\r\n" +
   "          font-size:" +
   "15px;\r\n" +
   "          font-weigh" +
   "t:bold;\r\n" +
   "          padding:9p" +
   "x 23px;\r\n" +
   "          text-decor" +
   "ation:none;\r\n" +
   "          text-shado" +
   "w:0px -1px 0px #7a2a" +
   "1d;\r\n" +
   "        }\r\n" +
   "        .myButton:ho" +
   "ver {\r\n" +
   "          background" +
   ":linear-gradient(to " +
   "bottom, #b34332 5%, " +
   "#a73f2d 100%);\r\n" +
   "          background" +
   "-color:#b34332;\r\n" +
   "        }\r\n" +
   "        .myButton:ac" +
   "tive {\r\n" +
   "          position:r" +
   "elative;\r\n" +
   "          top:1px;\r" +
   "\n" +
   "        }\r\n" +
   "  </style>\r\n" +
   "  \r\n\r\n" +
   "\t<script type=\"mod" +
   "ule\">\r\n\r\n" +
   "\t\tdocument.ready =" +
   " function () {\r\n" +
   "\t\t}\r\n" +
   "    \r\n" +
   "    document.onmouse" +
   "leave = function(){" +
   "\r\n" +
   "      Window.this.xc" +
   "all(\"GameFocus\");" +
   "\r\n" +
   "    }\r\n" +
   "\t</script>\r\n" +
   "  \r\n" +
   "</head>\r\n" +
   "  <body>\r\n" +
   "    <div id=\"app\" " +
   ">\r\n" +
   "        <a id=\"clos" +
   "e\" v-on:click=\"Clo" +
   "se()\" class=\"tsize" +
   "\">&#x2715;</a>\r\n" +
   "        <h1 class=\"" +
   "center\"> MUO + VueJ" +
   "s Demo</h1>\r\n" +
   "      <ul>\r\n" +
   "        <li v-for=\"" +
   "(item, index) in sho" +
   "pItems\">\r\n" +
   "          <p>{{ item" +
   ".Name }} amount: {{ " +
   "item.Count }} \r\n" +
   "            <a href=" +
   "\"#\" class=\"myButt" +
   "on\" v-on:click=\"Ge" +
   "t(index)\">OK</a>\r" +
   "\n" +
   "          </p>\r\n" +
   "        </li>\r\n" +
   "      </ul>\r\n" +
   "  </div>\r\n" +
   "  <script src=\"http" +
   "s://unpkg.com/vue\">" +
   "</script>\r\n" +
   "  <!-- <script src=" +
   "\"js\\vue.js\"></scr" +
   "ipt> -->\r\n" +
   "  <script>\r\n" +
   "      var app = new " +
   "Vue({\r\n" +
   "          el: \'#app" +
   "\',\r\n" +
   "          data: {\r" +
   "\n" +
   "               shopI" +
   "tems: []\r\n" +
   "          },\r\n" +
   "          beforeMoun" +
   "t(){\r\n" +
   "             //set s" +
   "ize \r\n" +
   "             Window." +
   "this.move(100,100, 5" +
   "00, 500, false);\r\n" +
   "             this.ge" +
   "tItems();\r\n" +
   "          },\r\n" +
   "          methods: {" +
   "\r\n" +
   "            getItems" +
   ": function () {\r\n" +
   "              Window" +
   ".this.xcall(\"SEND\"" +
   ", 1 ,function (value" +
   ") {\r\n" +
   "                  //" +
   "you can\'t use this " +
   "now it\'s func\r\n" +
   "                  ap" +
   "p.shopItems = value." +
   "ShopItems;\r\n" +
   "\t\t\t        });\r" +
   "\n" +
   "            },\r\n" +
   "            Get: fun" +
   "ction (index) {\r\n" +
   "              Window" +
   ".this.xcall(\"SEND\"" +
   ", 2, index);\r\n" +
   "            },\r\n" +
   "            Close: f" +
   "unction () {\r\n" +
   "                docu" +
   "ment.body.innerHTML " +
   "= \"\";\r\n" +
   "                Wind" +
   "ow.this.state =  Win" +
   "dow.WINDOW_HIDDEN;\r" +
   "\n" +
   "            },\r\n" +
   "          }\r\n" +
   "      });\r\n" +
   "  </script>\r\n" +
   "  </body>\r\n" +
   "</html>";
        }

    }
}
