namespace EcommerceAPI.Core.CrossCuttingConcerns.Logging;

public class LogDetail
{
    public string MethodName { get; set; }
    public List<LogParameter> LogParameters { get; set; }
}

public class LogParameter
{
    public string Name { get; set; }
    public object Value { get; set; }
    public string Type { get; set; }
}
