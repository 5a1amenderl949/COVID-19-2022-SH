using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/data", () =>
{
    var generator = new Generator();
    //WebContentFormattor.Format1("testformat-1.txt");
    return generator.Analyse();
});

app.MapGet("/edata", () =>
{
    var data = File.ReadAllText("./data.json");
    return JsonConvert.SerializeObject(data, Formatting.Indented);
});

app.Run();


internal class Generator
{
    internal static string root = @"data";
    internal static string[] xzq = { "徐汇区", "闵行区", "浦东新区", "黄浦区", "静安区", "长宁区", "虹口区", "杨浦区", "普陀区", "宝山区", "嘉定区", "金山区", "松江区", "青浦区", "奉贤区", "崇明区" };
    internal static string xhq = @"徐汇区";
    internal static string mhq = @"闵行区";
    internal static string pdxq = @"浦东新区";
    internal static string hpq = @"黄浦区";
    internal static string jaq = @"静安区";
    internal static string cnq = @"长宁区";
    internal static string hkq = @"虹口区";
    internal static string ypq = @"杨浦区";
    internal static string ptq = @"普陀区";
    internal static string bsq = @"宝山区";
    internal static string jdq = @"嘉定区";
    internal static string jsq = @"金山区";
    internal static string sjq = @"松江区";
    internal static string qpq = @"青浦区";
    internal static string fxq = @"奉贤区";
    internal static string cmq = @"崇明区";

    internal static string TtPartten = @"新增(本土)?((\d*)例)?新冠肺炎(本土)?确诊病例((\d*)例)?和((\d*)例本土)?无症状感染者((\d*)例)?";
    internal static string TtPart1Partten = @"(无?)新增本土新冠肺炎确诊病例((\d*)例)?";
    internal static string TtPart2Partten = @"新增本土无症状感染者((\d*)例)?";
    // internal static string regionPartten = @"，((\w*)区)((无?)新增|新增(\d*)例)本土(新冠肺炎)?确诊病例(、|，)(新增)?(\d*)例(本土)?无症状感染者";
    internal static string regionPartten = @"，{0}((无?)新增|新增(\d*)例)本土(新冠肺炎)?确诊病例(、|，)(新增)?(\d*)例(本土)?无症状感染者";

    public Result Analyse()
    {
        var root = GetDataFolder(new DirectoryInfo(Directory.GetCurrentDirectory()));
        DirectoryInfo directoryInfo = new DirectoryInfo(root);
        foreach (FileInfo file in directoryInfo.GetFiles())
        {
            WebContentFormattor.Format0(file.FullName);
            WebContentFormattor.Format1(file.FullName);
            WebContentFormattor.Format2(file.FullName);
        }

        List<Data> dataList = new List<Data>();
        List<AmountOfDay> dailyAmount = new List<AmountOfDay>();

        foreach (FileInfo file in directoryInfo.GetFiles())
        {
            Data data = new Data() { Day = file.Name.Replace(file.Extension, "").Replace('-', '.') };

            string contents = File.ReadAllText(file.FullName);
            var total = GetTotal(contents);
            dailyAmount.Add(new AmountOfDay() { Day = data.Day, Amount = total });
            data.Region = GetAddress(contents);
            dataList.Add(data);
        }

        dataList.Sort((a, b) => Convert.ToDouble(a.Day.Replace(".", "")) > Convert.ToDouble(b.Day.Replace(".", "")) ? 1 : -1);

        Dictionary<string, List<AmountOfDay>> increasment = new Dictionary<string, List<AmountOfDay>>();

        Result result = new Result() { Details = dataList, Amounts = dailyAmount };
        var resultStr = JsonConvert.SerializeObject(result);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "data.json"), resultStr);
        return result;
    }

    private string GetDataFolder(DirectoryInfo currentDir)
    {
        var subDirs = currentDir.GetDirectories();
        if (subDirs.ToList().Any(d => d.Name.ToLower() == "data")) return Path.Combine(currentDir.FullName, "data");
        return GetDataFolder(currentDir.Parent);
    }

    public int GetTotal(string contents)
    {
        string qzM = "0", wzM = "0";
        Match m = Regex.Match(contents, TtPartten);
        if (m.Success)
        {
            qzM = !string.IsNullOrEmpty(m.Groups[3].Value) ? m.Groups[3].Value : m.Groups[6].Value;
            wzM = !string.IsNullOrEmpty(m.Groups[8].Value) ? m.Groups[8].Value : m.Groups[10].Value;
            return Convert.ToInt32(qzM) + Convert.ToInt32(wzM);
        }
        m = Regex.Match(contents, TtPart1Partten);
        if (m.Success)
        {
            qzM = !string.IsNullOrEmpty(m.Groups[3].Value) ? m.Groups[3].Value : "0";
        }
        m = Regex.Match(contents, TtPart2Partten);
        if (m.Success)
        {
            wzM = !string.IsNullOrEmpty(m.Groups[2].Value) ? m.Groups[2].Value : "0"; 
        }
        return Convert.ToInt32(qzM) + Convert.ToInt32(wzM);
    }

    private List<Region> GetAddress(string contents)
    {
        List<Region> result = new List<Region>();
        foreach (string q in xzq)
        {
            Region address = new Region() { Name = q };
            address.Addresses = GetAddressIn(contents, q);
            var amt = GetRegionAmount(contents, q);
            address.Amount = amt == 0 ? address.Addresses.Count : amt;
            result.Add(address);
        }
        return result;
    }

    private int GetRegionAmount(string contents, string q)
    {
        string partten = string.Format(regionPartten, q);
        Match m = Regex.Match(contents, partten);
        if (m.Success)
        {
            var qzM = !string.IsNullOrEmpty(m.Groups[3].Value) ? m.Groups[3].Value : "0";
            var wzM = !string.IsNullOrEmpty(m.Groups[7].Value) ? m.Groups[7].Value : "0";
            return Convert.ToInt32(qzM) + Convert.ToInt32(wzM);
        }
        return 0;
    }

    public List<string> GetAddressIn(string contents, string q)
    {
        List<string> result = new List<string>();
        var partten = $"\\n({q}(\\w*))(，|、|。)";
        Match m = Regex.Match(contents, partten);
        var loopdog = 0;
        while (m.Success && loopdog++ < 10000)
        {
            result.Add(m.Groups[1].Value);
            m = m.NextMatch();
        }
        return result;
    }

    public static void testRegx()
    {
    }
}

internal static class WebContentFormattor
{
    public static void Format1(string path)
    {
        var format = "format-1";
        if (!path.Contains(format)) return;

        string newContents = "";

        var lines = File.ReadAllLines(path);
        var prefix = "";
        foreach (var line in lines)
        {
            var clearLine = line.Trim();
            if (string.Empty == clearLine) continue;
            if (Generator.xzq.Contains(clearLine))
            {
                prefix = clearLine;
                continue;
            }
            if (line.StartsWith("2022"))
            {
                newContents += line + "\n";
                continue;
            }
            if (clearLine.Contains("落实终末消毒"))
            {
                prefix = "";
                continue;
            }
            var newLine = prefix + clearLine;
            if (newLine.Contains("、"))
            {
                var adds = newLine.Split("、") ;
                newLine = adds[0] + "，\n";
                for (int i = 1; i < adds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(adds[i].Trim())) newLine += $"{prefix}" + adds[i]+"，\n";
                }
            }
            newLine = newLine.Replace("（住宅）", "");
            newContents += newLine + "，\n";
        }

        File.WriteAllText(path.Replace(format, ""), newContents);
        File.Delete(path);
    }

    public static void Format0(string path)
    {
        var format = "format-0";
        if (!path.Contains(format)) return;

        string newContents = "";

        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            var clearLine = line.Trim();
            if (string.Empty == clearLine) continue;
            var newLine = clearLine.Replace("居住于", "\n");
            newLine = newLine.Replace("居住地为", "\n");
            // newContents += newLine.Replace("无症状", "\n无症状");
            newContents += newLine;
        }

        File.WriteAllText(path.Replace(format, ""), newContents);
        File.Delete(path);
    }

    public static void Format2(string path)
    {
        var format = "format-2";
        if (!path.Contains(format)) return;

        string newContents = "";

        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            var clearLine = line.Trim();
            if (string.Empty == clearLine) continue;
            var newLine = clearLine.Replace("居住于", "\n");
            newLine = newLine.Replace("居住地为", "\n");
            // newContents += newLine.Replace("无症状", "\n无症状");
            newContents += newLine;
        }

        File.WriteAllText(path.Replace(format, ""), newContents);
        File.Delete(path);
    }
}

internal class Data
{
    public string Day { get; set; }
    public List<Region> Region { get; set; }
}

internal class Region
{
    public string Name { get; set; }
    public List<string> Addresses { get; set; }
    public int Amount { get; set; }
}

internal class AmountOfDay
{
    public string Day { get; set; }
    public int Amount { get; set; }
}

internal class Result
{
    public List<Data> Details { get; set; }
    public List<AmountOfDay> Amounts { get; set; }

}

internal static class StringExtension
{
    /// <summary>
    /// <summary>
    /// 字符串转Unicode
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <returns>Unicode编码后的字符串</returns>
    public static string String2Unicode(this string source)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(source);
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 2)
        {
            stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Unicode转字符串
    /// </summary>
    /// <param name="source">经过Unicode编码的字符串</param>
    /// <returns>正常字符串</returns>
    public static string Unicode2String(this string source)
    {
        return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
                     source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
    }
}