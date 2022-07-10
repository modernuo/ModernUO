using System;
using Server.Network;

namespace Server.Gumps;

public class SciterTestGump : Gump
{
    public record Item(string Name, int Count);

    public class Shop
    {
        public Item[] ShopItems { get; } = {
            new("Arrow", 10),
            new("Bolt", 10),
            new("Gold", 20000),
        };
    }
    private Shop _shop = new();

    public SciterTestGump(Mobile user) : base(0, 0)
    {
        UseWebRender = true;
        AddBody(Body);
    }

    public void TryToAddItem(Mobile m, int index)
    {
        if (_shop.ShopItems.Length < index)
        {
            return;
        }

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
                {
                    sender.RemoveGump(this);
                    break;
                }
            case 1:
                {
                    SendData(from, "1", _shop);
                    break;
                }
            case 2:
                {
                    TryToAddItem(from, Utility.ToInt32(info.TextEntries[0].Text));
                    break;
                }
        }

    }

    private const string Body = @"<html>
<head>
  <style>
      html { background-color: black; }
      h1 { color: white; }
      p { color: white; }
      .btn1 {
        background-color: yellow;
        color: black;
        text-align: center;
        font-size: 13px;
      }

      #close {
        position: relative;
        float: right;
      }

      .center {
        position: relative;
        float: center;
      }

      .close-btn {
        font-size: 60px;
        font-weight: bold;
        color: #000;
      }

      .tsize
      {
        color: red;
        font-size: 18px;
      }

      .myButton {
        box-shadow: inset 0px 34px 0px -15px #b54b3a;
        background: linear-gradient(to bottom, #a73f2d 5%, #b34332 100%);
        background-color: #a73f2d;
        border: 1px solid #241d13;
        display: inline-block;
        cursor: pointer;
        color: #ffffff;
        font-family: Arial;
        font-size: 15px;
        font-weight: bold;
        padding: 9px 23px;
        text-decoration: none;
        text-shadow: 0px -1px 0px #7a2a1d;
      }
      .myButton:hover {
        background: linear-gradient(to bottom, #b34332 5%, #a73f2d 100%);
        background-color: #b34332;
      }
      .myButton:active {
        position: relative;
        top: 1px;
      }
  </style>
  <script type=""module"">

    document.ready = function () {}
    document.onmouseleave = function(){
      Window.this.xcall('GameFocus');
    }
  </script>
</head>
<body>
  <div id=""app"" >
      <a id=""close"" v-on:click=""Close()"" class=""tsize"">&#x2715;</a>
      <h1 class=""center""> MUO + VueJs Demo</h1>
    <ul>
      <li v-for=""(item, index) in shopItems"">
        <p>{{ item.Name }} amount: {{ item.Count }}
          <a href=""#"" class=""myButton"" v-on:click=""Get(index)"">OK</a>
        </p>
      </li>
    </ul>
</div>
  <script src=""https://unpkg.com/vue""></script>
  <script>
    var app = new Vue({
      el: '#app',
      data: { shopItems: [] },
      beforeMount: function () {
        //set size
        Window.this.move(100, 100, 500, 500, false);
        this.getItems();
      },
      methods: {
        getItems: function () {
          Window.this.xcall('SEND', 1, function (value) {
            //you can't use this now it's func
            app.shopItems = value.ShopItems;
          });
        },
        Get: function (index) {
          Window.this.xcall('SEND', 2, index);
        },
        Close: function () {
            document.body.innerHTML = """";
            Window.this.state =  Window.WINDOW_HIDDEN;
        },
      }
    });
  </script>
</body>
</html>";
}
