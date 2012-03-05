﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.IO;
using UberDeployer.Core.Domain;

namespace UberDeployer.Core.DataAccess.Xml
{
  public class XmlEnvironmentInfoRepository : IEnvironmentInfoRepository
  {
    private static readonly Regex _EnvironmentVariableRegex = new Regex("%{(?<VariableName>[^}]+)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #region Nested types

    public class EnvironmentInfosXml
    {
      public List<EnvironmentInfoXml> EnvironmentInfos { get; set; }
    }

    public class EnvironmentInfoXml
    {
      public string Name { get; set; }

      public string ConfigurationTemplatesName { get; set; }

      public string AppServerMachineName { get; set; }

      public string WebServerMachineName { get; set; }

      public string TerminalServerMachineName { get; set; }

      public string DatabaseServerMachineName { get; set; }

      public string NtServicesBaseDirPath { get; set; }

      public string WebAppsBaseDirPath { get; set; }

      public string SchedulerAppsBaseDirPath { get; set; }

      public string TerminalAppsBaseDirPath { get; set; }

      public List<EnvironmentUserXml> EnvironmentUsers { get; set; }
    }

    public class EnvironmentUserXml
    {
      public string Id { get; set; }

      public string UserName { get; set; }
    }

    #endregion

    private readonly string _xmlFilePath;

    private EnvironmentInfosXml _environmentInfosXml;
    private Dictionary<string, EnvironmentInfo> _environmentInfosByName;

    #region Constructor(s)

    public XmlEnvironmentInfoRepository(string xmlFilePath)
    {
      if (string.IsNullOrEmpty(xmlFilePath))
      {
        throw new ArgumentException("Argument can't be null nor empty.", "xmlFilePath");
      }

      _xmlFilePath = xmlFilePath;
    }

    #endregion

    #region IEnvironmentInfoRepository Members

    public IEnumerable<EnvironmentInfo> GetAll()
    {
      LoadXmlIfNeeded();

      return _environmentInfosByName.Values
        .OrderByDescending(ei => ei.Name); // TODO IMM HI: OrderBy instead of OrderByDescending
    }

    public EnvironmentInfo GetByName(string environmentName)
    {
      if (string.IsNullOrEmpty(environmentName))
      {
        throw new ArgumentException("Argument can't be null nor empty.", "environmentName");
      }

      LoadXmlIfNeeded();

      EnvironmentInfo environmentInfo;

      if (!_environmentInfosByName.TryGetValue(environmentName, out environmentInfo))
      {
        return null;
      }

      return environmentInfo;
    }

    #endregion

    #region Private helper methods

    private static string ExpandEnvironmentVariables(string s)
    {
      if (s == null)
      {
        throw new ArgumentNullException("s");
      }

      if (s.Length == 0)
      {
        return s;
      }

      return
        _EnvironmentVariableRegex.Replace(
          s,
          match =>
          {
            string variableName = match.Groups["VariableName"].Value;

            return Environment.GetEnvironmentVariable(variableName) ?? "";
          });
    }

    private void LoadXmlIfNeeded()
    {
      if (_environmentInfosXml != null)
      {
        return;
      }

      var xmlSerializer = new XmlSerializer(typeof(EnvironmentInfosXml));

      using (var fs = File.OpenRead(_xmlFilePath))
      {
        _environmentInfosXml = (EnvironmentInfosXml)xmlSerializer.Deserialize(fs);
      }

      _environmentInfosByName =
        _environmentInfosXml.EnvironmentInfos
          .Select(
            eiXml =>
            new EnvironmentInfo(
              ExpandEnvironmentVariables(eiXml.Name),
              ExpandEnvironmentVariables(eiXml.ConfigurationTemplatesName),
              ExpandEnvironmentVariables(eiXml.AppServerMachineName),
              ExpandEnvironmentVariables(eiXml.WebServerMachineName),
              ExpandEnvironmentVariables(eiXml.TerminalServerMachineName),
              ExpandEnvironmentVariables(eiXml.DatabaseServerMachineName),
              ExpandEnvironmentVariables(eiXml.NtServicesBaseDirPath),
              ExpandEnvironmentVariables(eiXml.WebAppsBaseDirPath),
              ExpandEnvironmentVariables(eiXml.SchedulerAppsBaseDirPath),
              ExpandEnvironmentVariables(eiXml.TerminalAppsBaseDirPath),
              eiXml.EnvironmentUsers.Select(
                eu =>
                new EnvironmentUser(
                  ExpandEnvironmentVariables(eu.Id),
                  ExpandEnvironmentVariables(eu.UserName)))))
          .ToDictionary(ei => ei.Name);
    }

    #endregion
  }
}