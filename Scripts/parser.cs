using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml;
using System.Text;

public enum uiStruType
{
    DIV,
    IMG,
    TEXT,
    BUTTON
}

public class uiStruBase
{
    public string id;
    public string _class;

    public uiStruType type;

    public uiStruBase() { }
    public uiStruBase(
        string _id,
        string __class,
        uiStruType _type)
    {
        id = _id;
        _class = __class;
        type = _type;
    }

}

public class uiStruImg : uiStruBase
{
    public string src;

    public uiStruImg() { }
    public uiStruImg(
        string _id,
        string __class,
        string _src) : base(_id, __class, uiStruType.IMG)
    {
        src = _src;
    }
}

public class uiStruText : uiStruBase
{
    public string content;

    public uiStruText() { }
    public uiStruText(
        string _id,
        string __class,
        string _content) : base(_id, __class, uiStruType.TEXT)
    {
        content = _content;
    }
}

public class uiStruButton : uiStruBase
{
    public string content;

    public uiStruButton() { }
    public uiStruButton(
        string _id,
        string __class,
        string _content) : base(_id, __class, uiStruType.BUTTON)
    {
        content = _content;
    }
}

public class uiStruDiv : uiStruBase
{
    public List<uiStruBase> nodes = new List<uiStruBase>();

    public uiStruDiv() { }
    public uiStruDiv(
        string _id,
        string __class,
        List<uiStruBase> _nodes) : base(_id, __class, uiStruType.DIV)
    {
        nodes = _nodes;
    }
}

public static class TransformEx
{
    public static void ResetTransform(this Transform _transform)
    {
        _transform.localPosition = Vector3.zero;
        _transform.localEulerAngles = Vector3.zero;
        _transform.localScale = Vector3.one;
    }
}

public class parser : MonoBehaviour
{
    private uiStruDiv tree;
    private Dictionary<string, uiStruBase> treeMap = new Dictionary<string, uiStruBase>();
    private Dictionary<string, GameObject> activeTree = new Dictionary<string, GameObject>();
    private Dictionary<string, List<string>> _classTree = new Dictionary<string, List<string>>();

    // todo: 临时使用的预制体
    [SerializeField]
    private GameObject _image;
    [SerializeField]
    private GameObject _text;
    [SerializeField]
    private GameObject _button;
    [SerializeField]
    private GameObject _div;

    // Start is called before the first frame update
    void Start()
    {
        parserXML();
        spawnUI();
        cssSetting();
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region layout

    void spawnUI()
    {
        spawnDIV(tree, transform);
    }

    Transform spawnItem(GameObject obj, uiStruBase node, Transform _transform)
    {
        GameObject item = Instantiate(obj, _transform);
        item.transform.ResetTransform();

        activeTree[node.id] = item;
        if (!_classTree.ContainsKey(node._class))
            _classTree[node._class] = new List<string>();

        if (!_classTree[node._class].Contains(node.id))
            _classTree[node._class].Add(node.id);

        return item.transform;
    }

    Transform spawnDIV(uiStruDiv node, Transform _transform)
    {
        Transform div = spawnItem(_div, node, _transform);
        foreach (var childNode in node.nodes)
        {
            switch (childNode.type)
            {
                case uiStruType.IMG:
                    spawnIMG(childNode as uiStruImg, div);
                    break;
                case uiStruType.BUTTON:
                    spawnBUTTON(childNode as uiStruButton, div);
                    break;
                case uiStruType.TEXT:
                    spawnTEXT(childNode as uiStruText, div);
                    break;
                case uiStruType.DIV:
                    spawnDIV(childNode as uiStruDiv, div);
                    break;
                default:
                    break;
            }
        }

        return div;
    }

    Transform spawnIMG(uiStruImg uiItem, Transform _transform)
    {
        Transform img = spawnItem(_image, uiItem, _transform);

        Image image = img.GetComponent<Image>();

        // todo: join
        string[] srcList = uiItem.src.Split('/');

        if (srcList[0] == "R")
        {
            string url = srcList[1];
            Sprite sprite = Resources.Load<Sprite>(url);
            image.sprite = sprite;
        }

        return img;
    }

    Transform spawnTEXT(uiStruText uiItem, Transform _transform)
    {
        Transform text = spawnItem(_text, uiItem, _transform);

        Text txt = text.GetComponent<Text>();
        txt.text = uiItem.content;

        return text;
    }

    Transform spawnBUTTON(uiStruButton uiItem, Transform _transform)
    {
        Transform button = spawnItem(_button, uiItem, _transform);

        Text txt = button.GetComponentInChildren<Text>();
        txt.text = uiItem.content;

        return button;
    }

    #endregion

    #region css

    public class Property
    {
        public string display { get; set; }
        public string position { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public string width { get; set; }
        public string height { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var property in GetType().GetProperties())
            {
                sb.Append(property.Name + ": " + property.GetValue(this) + " ");
            }
            return sb.ToString();
        }
    }

    private Dictionary<string, Property> propertyDic = new Dictionary<string, Property>();

    enum cssParseStatus
    {
        DONE,
        CLASSNAME,
        PROPERTY
    }

    void cssSetting()
    {
        TextAsset css = Resources.Load<TextAsset>("uicss");

        string rawCss = css.text.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");

        cssParse(rawCss);
        cssSet4Tree();
        //Debug.Log(rawCss);
    }

    void cssParse(string rawCss)
    {
        StringBuilder curSB = new StringBuilder();
        cssParseStatus status = cssParseStatus.DONE;
        string curClassName = "";
        string curProperty = "";

        for (int i = 0; i < rawCss.Length; ++i)
        {
            if (status == cssParseStatus.DONE)
            {
                if (rawCss[i] == '.')
                {
                    curSB.Clear();
                    status = cssParseStatus.CLASSNAME;
                    continue;
                }
            }

            else if (status == cssParseStatus.CLASSNAME)
            {
                if (rawCss[i] == '{')
                {
                    status = cssParseStatus.PROPERTY;
                    curClassName = curSB.ToString();
                    propertyDic[curClassName] = new Property();
                    curSB.Clear();
                    continue;
                }
                curSB.Append(rawCss[i]);
            }

            else if (status == cssParseStatus.PROPERTY)
            {
                if (rawCss[i] == ':')
                {
                    curProperty = curSB.ToString();
                    curSB.Clear();
                    continue;
                }

                if (rawCss[i] == ';')
                {
                    //Debug.Log(curProperty);
                    //Debug.Log(curClassName);
                    //Debug.Log(curSB.ToString());

                    propertyDic[curClassName].GetType().GetProperty(curProperty)
                        .SetValue(propertyDic[curClassName], curSB.ToString());
                    curProperty = "";
                    curSB.Clear();
                    continue;
                }

                if (rawCss[i] == '}')
                {
                    curSB.Clear();
                    status = cssParseStatus.DONE;
                    curClassName = "";

                    continue;
                }

                curSB.Append(rawCss[i]);
            }
        }

        //foreach (var pair in propertyDic)
        //{
        //    Debug.Log(pair.Key + ": " + pair.Value.ToString());
        //}
    }

    // todo:
    // margin padding
    // 百分比布局
    // flex
    void cssSet4Tree()
    {
        foreach (var pair in propertyDic)
        {
            Debug.Log(pair.Key + ": " + pair.Value.ToString());
            if (!_classTree.ContainsKey(pair.Key))
                continue;

            List<string> ids = _classTree[pair.Key];
            foreach (string id in ids)
            {
                GameObject go = activeTree[id];
                Property property = pair.Value;

                RectTransform rect = go.GetComponent<RectTransform>();

                // todo: 绝对布局直接设置
                if (property.position == "absolute")
                {
                    rect.localPosition = new Vector3(float.Parse(property.x), float.Parse(property.y), 1);
                }
                float temp;
                if (float.TryParse(property.width, out temp))
                {
                    rect.sizeDelta = new Vector2(float.Parse(property.width), rect.sizeDelta.y);
                }
                if (float.TryParse(property.height, out temp))
                {
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, float.Parse(property.height));
                }

                // flex布局直接设置所有子物体在水平面排成一排
                if (property.display == "flex")
                {
                    uiStruDiv div = treeMap[id] as uiStruDiv;

                    float childX = 0f;

                    foreach (var node in div.nodes)
                    {
                        GameObject childGo = activeTree[node.id];
                        bool isHasChildProperty = propertyDic.ContainsKey(node._class);
                        Property childProperty = new Property();
                        if (isHasChildProperty)
                        {
                            childProperty = propertyDic[node._class];
                        }

                        RectTransform childRect = childGo.GetComponent<RectTransform>();

                        if (isHasChildProperty && childProperty.position == "absolute")
                        {
                            continue;
                        }

                        childRect.localPosition = new Vector3(
                            childX,
                            rect.sizeDelta.y / 2 -
                               (!isHasChildProperty || !float.TryParse(childProperty.height, out temp) ? childRect.sizeDelta.y : float.Parse(childProperty.height)) / 2, 1);
                        childX += rect.sizeDelta.x;
                    }
                }
            }
        }
    }

    #endregion

    #region parser

    bool validNULL(string val)
    {
        if (val == string.Empty) return true;
        return false;
    }

    /// <summary>
    /// xml解析方法
    /// </summary>
    void parserXML()
    {
        //TextAsset xml = Resources.Load<TextAsset>("ui");

        XmlDocument doc = new XmlDocument();
        doc.Load(Application.dataPath + "/Resources/ui.xml");
        XmlElement rootNode = doc.LastChild as XmlElement;
        if (rootNode.Name != "frame")
        {
            Debug.LogError("please name root is 'frame'");
            return;
        }

        XmlElement uiNode = rootNode.FirstChild as XmlElement;

        if (uiNode.Name != "ui")
        {
            Debug.LogError("please name realroot is 'ui'");
            return;
        }

        tree = new uiStruDiv("root", "root", ForeachEleNodes(uiNode));
        treeMap["root"] = tree;

    }

    /// <summary>
    /// 遍历所有节点
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    List<uiStruBase> ForeachEleNodes(XmlElement parent)
    {
        List<uiStruBase> nodes = new List<uiStruBase>();
        foreach (XmlElement uiItem in parent.ChildNodes)
        {
            if (uiItem.Name == "img")
            {
                nodes.Add(parserImg(uiItem));
            }
            if (uiItem.Name == "div")
            {
                nodes.Add(parserDiv(uiItem));
            }
            if (uiItem.Name == "text")
            {
                nodes.Add(parserText(uiItem));
            }
            if (uiItem.Name == "button")
            {
                nodes.Add(parserButton(uiItem));
            }
        }

        return nodes;
    }

    /// <summary>
    /// xml-div解析方法
    /// </summary>
    /// <param name="uiItem"></param>
    uiStruBase parserDiv(XmlElement uiItem)
    {
        string id = uiItem.GetAttribute("id");
        string _class = uiItem.GetAttribute("class");

        if (validNULL(id) || validNULL(_class))
        {
            Debug.LogError("xml div ele too less!");
            return null;
        }

        List<uiStruBase> nodes = ForeachEleNodes(uiItem);

        uiStruDiv div = new uiStruDiv(id, _class, nodes);
        treeMap[id] = div;

        return div;
    }

    /// <summary>
    /// xml-text解析方法
    /// </summary>
    /// <param name="uiItem"></param>
    uiStruBase parserText(XmlElement uiItem)
    {
        string id = uiItem.GetAttribute("id");
        string _class = uiItem.GetAttribute("class");
        string content = uiItem.InnerText;

        if (validNULL(id) || validNULL(_class) || validNULL(content))
        {
            Debug.LogError("xml text ele too less!");
            return null;
        }

        uiStruText text = new uiStruText(id, _class, content);
        treeMap[id] = text;

        return text;
    }

    /// <summary>
    /// xml-button解析方法
    /// </summary>
    /// <param name="uiItem"></param>
    uiStruBase parserButton(XmlElement uiItem)
    {
        string id = uiItem.GetAttribute("id");
        string _class = uiItem.GetAttribute("class");
        string content = uiItem.InnerText;
        if (validNULL(id) || validNULL(_class) || validNULL(content))
        {
            Debug.LogError("xml button ele too less!");
            return null;
        }

        uiStruButton button = new uiStruButton(id, _class, content);
        treeMap[id] = button;

        return button;
    }

    /// <summary>
    /// xml-img解析方法
    /// </summary>
    /// <param name="uiItem"></param>
    uiStruBase parserImg(XmlElement uiItem)
    {
        string id = uiItem.GetAttribute("id");
        string _class = uiItem.GetAttribute("class");
        string src = uiItem.GetAttribute("src");

        if (validNULL(id) || validNULL(_class) || validNULL(src))
        {
            Debug.LogError("xml img ele too less!");
            return null;
        }

        uiStruImg img = new uiStruImg(id, _class, src);
        treeMap[id] = img;

        return img;
    }

    #endregion
}
