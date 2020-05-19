﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using Newtonsoft.Json;
using TeamCitySharp.Connection;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;
using static System.Boolean;

namespace TeamCitySharp.ActionTypes
{
  public class BuildConfigs : IBuildConfigs
  {
    private readonly ITeamCityCaller m_caller;
    private string m_fields;

    internal BuildConfigs(ITeamCityCaller caller)
    {
      m_caller = caller;
    }

    public BuildConfigs GetFields(string fields)
    {
      var newInstance = (BuildConfigs) MemberwiseClone();
      newInstance.m_fields = fields;
      return newInstance;
    }

    public List<BuildConfig> All()
    {
      var buildType =
        m_caller.Get<BuildTypeWrapper>(ActionHelper.CreateFieldUrl("/buildTypes", m_fields));

      return buildType.BuildType;
    }

    public BuildConfig ByConfigurationName(string buildConfigName)
    {
      var build = m_caller.GetFormat<BuildConfig>(ActionHelper.CreateFieldUrl("/buildTypes/name:{0}", m_fields),
                                                 buildConfigName);

      return build;
    }

    public BuildConfig ByConfigurationId(string buildConfigId)
    {
      var build = m_caller.GetFormat<BuildConfig>(ActionHelper.CreateFieldUrl("/buildTypes/id:{0}", m_fields),
                                                 buildConfigId);

      return build;
    }

    public BuildConfig ByProjectNameAndConfigurationName(string projectName, string buildConfigName)
    {
      var build =
        m_caller.Get<BuildConfig>(
          ActionHelper.CreateFieldUrl(
            $"/projects/name:{projectName}/buildTypes/name:{buildConfigName}", m_fields));
      return build;
    }

    public BuildConfig ByProjectNameAndConfigurationId(string projectName, string buildConfigId)
    {
      var build =
        m_caller.Get<BuildConfig>(
          ActionHelper.CreateFieldUrl(
            $"/projects/name:{projectName}/buildTypes/id:{buildConfigId}", m_fields));
      return build;
    }

    public BuildConfig ByProjectIdAndConfigurationName(string projectId, string buildConfigName)
    {
      var build =
        m_caller.Get<BuildConfig>(
          ActionHelper.CreateFieldUrl(
            $"/projects/id:{projectId}/buildTypes/name:{Uri.EscapeDataString(buildConfigName)}", m_fields));
      return build;
    }

    public BuildConfig ByProjectIdAndConfigurationId(string projectId, string buildConfigId)
    {
      var build =
        m_caller.Get<BuildConfig>(
          ActionHelper.CreateFieldUrl(
            $"/projects/id:{projectId}/buildTypes/id:{buildConfigId}", m_fields));
      return build;
    }

    public List<BuildConfig> ByProjectId(string projectId)
    {
      var buildWrapper =
        m_caller.GetFormat<BuildTypeWrapper>(
          ActionHelper.CreateFieldUrl("/projects/id:{0}/buildTypes", m_fields), projectId);

      return buildWrapper?.BuildType ?? new List<BuildConfig>();
    }

    public List<BuildConfig> ByProjectName(string projectName)
    {
      var buildWrapper =
        m_caller.GetFormat<BuildTypeWrapper>(
          ActionHelper.CreateFieldUrl("/projects/name:{0}/buildTypes", m_fields), projectName);

      return buildWrapper?.BuildType ?? new List<BuildConfig>();
    }

    public BuildConfig CreateConfiguration(BuildConfig buildConfig)
    {
      return m_caller.PostFormat<BuildConfig>(buildConfig, HttpContentTypes.ApplicationJson,
        HttpContentTypes.ApplicationJson, "/buildTypes");
    }

    public BuildConfig CreateConfiguration(string projectName, string configurationName)
    {
      return m_caller.PostFormat<BuildConfig>(configurationName, HttpContentTypes.TextPlain,
                                             HttpContentTypes.ApplicationJson, "/projects/name:{0}/buildTypes",
                                             projectName);
    }

    public BuildConfig CreateConfigurationByProjectId(string projectId, string configurationName)
    {
      return m_caller.PostFormat<BuildConfig>(configurationName, HttpContentTypes.TextPlain,
                                             HttpContentTypes.ApplicationJson, "/projects/id:{0}/buildTypes",
                                             projectId);
    }

    internal HttpResponseMessage CopyBuildConfig(string buildConfigId, string buildConfigName, string destinationProjectId,
                                          string newBuildTypeId = "")
    {
      string xmlData;
      if (newBuildTypeId != "")
      {
        xmlData =
          string.Format(
            "<newBuildTypeDescription name='{0}' id='{2}' sourceBuildTypeLocator='id:{1}' copyAllAssociatedSettings='true' shareVCSRoots='false'/>",
            buildConfigName, buildConfigId, newBuildTypeId);
      }
      else
      {
        xmlData =
          $"<newBuildTypeDescription name='{buildConfigName}' sourceBuildTypeLocator='id:{buildConfigId}' copyAllAssociatedSettings='true' shareVCSRoots='false'/>";
      }
      var response = m_caller.Post(xmlData, HttpContentTypes.ApplicationXml,
        $"/projects/id:{destinationProjectId}/buildTypes",
                                  HttpContentTypes.ApplicationJson);
      return response;
    }

    public BuildConfig Copy(string buildConfigId, string buildConfigName, string destinationProjectId,
                            string newBuildTypeId = "")
    {
      var response = CopyBuildConfig(buildConfigId, buildConfigName, destinationProjectId, newBuildTypeId);
      if (response.StatusCode == HttpStatusCode.OK)
      {
        var buildConfig = JsonConvert.DeserializeObject<BuildConfig>(response.RawText());
        return buildConfig;
      }
      return new BuildConfig();
    }

    public Template CopyTemplate(string templateId, string templateName, string destinationProjectId,
                                 string newTemplateId = "")
    {
      var response = CopyTemplateQuery(templateId, templateName, destinationProjectId, newTemplateId);
      if (response.StatusCode == HttpStatusCode.OK)
      {
        var template = JsonConvert.DeserializeObject<Template>(response.RawText());
        return template;
      }
      return new Template();
    }

    private HttpResponseMessage CopyTemplateQuery(string templateId, string templateName, string destinationProjectId,
                                           string newTemplateId)
    {
      var xmlData = newTemplateId != ""
        ? $"<newBuildTypeDescription name='{templateName}' id='{newTemplateId}' sourceBuildTypeLocator='id:{templateId}' copyAllAssociatedSettings='true' shareVCSRoots='false'/>"
        : $"<newBuildTypeDescription name='{templateName}' sourceBuildTypeLocator='id:{templateId}' copyAllAssociatedSettings='true' shareVCSRoots='false'/>";
      var response = m_caller.Post(xmlData, HttpContentTypes.ApplicationXml,
        $"/projects/id:{destinationProjectId}/templates",
                                  HttpContentTypes.ApplicationJson);
      return response;
    }

    public void SetConfigurationSetting(BuildTypeLocator locator, string settingName, string settingValue)
    {
      m_caller.PutFormat(settingValue, HttpContentTypes.TextPlain, "/buildTypes/{0}/settings/{1}", locator,
                        settingName);
    }

    public bool GetConfigurationPauseStatus(BuildTypeLocator locator)
    {
      TryParse(
        m_caller.GetRaw(ActionHelper.CreateFieldUrl($"/buildTypes/{locator}/paused/", m_fields)), out var result);
      return result;
    }

    public void SetConfigurationPauseStatus(BuildTypeLocator locator, bool isPaused)
    {
      m_caller.PutFormat(isPaused, HttpContentTypes.TextPlain, "/buildTypes/{0}/paused/", locator);
    }

    public void PostRawArtifactDependency(BuildTypeLocator locator, string rawXml)
    {
      m_caller.PostFormat<ArtifactDependency>(rawXml, HttpContentTypes.ApplicationXml, HttpContentTypes.ApplicationJson,
                                             "/buildTypes/{0}/artifact-dependencies", locator);
    }

    public string GetRawBuildStep(BuildTypeLocator locator, string runner)
    {
        return m_caller.GetRawXml(ActionHelper.CreateFieldUrl($"/buildTypes/{locator}/steps/{runner}", m_fields));
    }

    public void PostRawBuildStep(BuildTypeLocator locator, string rawXml)
    {
      m_caller.PostFormat<BuildConfig>(rawXml, HttpContentTypes.ApplicationXml, HttpContentTypes.ApplicationJson,
                                      "/buildTypes/{0}/steps", locator);
    }

    public void PutRawBuildStep(BuildTypeLocator locator, string stepId, string value)
    {
        m_caller.PutFormat(value, HttpContentTypes.ApplicationXml, "/buildTypes/{0}/steps/{1}", locator, stepId);
    }

    public void PostRawBuildTrigger(BuildTypeLocator locator, string rawXml)
    {
      m_caller.PostFormat(rawXml, HttpContentTypes.ApplicationXml, "/buildTypes/{0}/triggers", locator);
    }

    public void SetArtifactDependency(BuildTypeLocator locator, ArtifactDependency dependency)
    {
      m_caller.PostFormat<ArtifactDependency>(dependency, HttpContentTypes.ApplicationJson,
                                             HttpContentTypes.ApplicationJson,
                                             "/buildTypes/{0}/artifact-dependencies", locator);
    }

    public void SetSnapshotDependency(BuildTypeLocator locator, SnapshotDependency dependency)
    {
      m_caller.PostFormat<SnapshotDependency>(dependency, HttpContentTypes.ApplicationJson,
                                             HttpContentTypes.ApplicationJson,
                                             "/buildTypes/{0}/snapshot-dependencies", locator);
    }

    public void SetTrigger(BuildTypeLocator locator, BuildTrigger trigger)
    {
      m_caller.PostFormat<BuildTrigger>(trigger, HttpContentTypes.ApplicationJson, HttpContentTypes.ApplicationJson,
                                       "/buildTypes/{0}/triggers", locator);
    }

    public void SetConfigurationParameter(BuildTypeLocator locator, string key, string value)
    {
      m_caller.PutFormat(value, HttpContentTypes.TextPlain, "/buildTypes/{0}/parameters/{1}", locator, key);
    }

    public void DeleteConfiguration(BuildTypeLocator locator)
    {
      m_caller.DeleteFormat("/buildTypes/{0}", locator);
    }

    public void DeleteAllBuildTypeParameters(BuildTypeLocator locator)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/parameters", locator);
    }

    public void PutAllBuildTypeParameters(BuildTypeLocator locator, IDictionary<string, string> parameters)
    {
      if (locator == null) throw new ArgumentNullException("locator");
      if (parameters == null) throw new ArgumentNullException("parameters");

      var sw = new StringWriter();
      using (var writer = new XmlTextWriter(sw))
      {
        writer.WriteStartElement("properties");
        foreach (var parameter in parameters)
        {
          writer.WriteStartElement("property");
          writer.WriteAttributeString("name", parameter.Key);
          writer.WriteAttributeString("value", parameter.Value);
          writer.WriteEndElement();
        }
        writer.WriteEndElement();
      }

      m_caller.PutFormat(sw.ToString(), HttpContentTypes.ApplicationXml, "/buildTypes/{0}/parameters", locator);
    }

    public void DownloadConfiguration(BuildTypeLocator locator, Action<string> downloadHandler)
    {
      var url = $"/buildTypes/{locator}";
      m_caller.GetDownloadFormat(downloadHandler, url);
    }

    public void PostRawAgentRequirement(BuildTypeLocator locator, string rawXml)
    {
      m_caller.PostFormat(rawXml, HttpContentTypes.ApplicationXml, "/buildTypes/{0}/agent-requirements", locator);
    }

    public void DeleteBuildStep(BuildTypeLocator locator, string buildStepId)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/steps/{1}", locator, buildStepId);
    }

    public void DeleteArtifactDependency(BuildTypeLocator locator, string artifactDependencyId)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/artifact-dependencies/{1}", locator, artifactDependencyId);
    }

    public void DeleteAgentRequirement(BuildTypeLocator locator, string agentRequirementId)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/agent-requirements/{1}", locator, agentRequirementId);
    }

    public void DeleteParameter(BuildTypeLocator locator, string parameterName)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/parameters/{1}", locator, parameterName);
    }

    public void DeleteBuildTrigger(BuildTypeLocator locator, string buildTriggerId)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/triggers/{1}", locator, buildTriggerId);
    }

    public void SetBuildTypeTemplate(BuildTypeLocator locatorBuildType, BuildTypeLocator locatorTemplate)
    {
      m_caller.PutFormat(locatorTemplate.ToString(), HttpContentTypes.TextPlain, "/buildTypes/{0}/template",
                        locatorBuildType);
    }

    public void DeleteSnapshotDependency(BuildTypeLocator locator, string snapshotDependencyId)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/snapshot-dependencies/{1}", locator, snapshotDependencyId);
    }

    public void PostRawSnapshotDependency(BuildTypeLocator locator, XmlElement rawXml)
    {
      m_caller.PostFormat(rawXml.OuterXml, HttpContentTypes.ApplicationXml,
                         "/buildTypes/{0}/snapshot-dependencies", locator);
    }

    public BuildConfig BuildType(BuildTypeLocator locator)
    {
      var build = m_caller.GetFormat<BuildConfig>(ActionHelper.CreateFieldUrl("/buildTypes/{0}", m_fields),
                                                 locator);

      return build;
    }

    public void SetBuildTypeVariable(BuildTypeLocator locatorBuildType, string nameVariable, string value)
    {
      m_caller.PutFormat(value, HttpContentTypes.TextPlain, "/buildTypes/{0}/{1}", locatorBuildType,
                        nameVariable);
    }

    public bool ModifTrigger(string buildTypeId, string triggerId, string newBt)
    {
      //Get data from the old trigger
      var urlExtractAllTriggersOld = $"/buildTypes/id:{buildTypeId}/triggers";
      var triggers = m_caller.GetFormat<BuildTriggers>(urlExtractAllTriggersOld);
      foreach (var trigger in triggers.Trigger.OrderByDescending(m => m.Id))
      {
        if (trigger.Type != "buildDependencyTrigger") continue;

        foreach (var property in trigger.Properties.Property)
        {
          if (property.Name != "dependsOn") continue;

          if (triggerId != property.Value) continue;

          property.Value = newBt;

          var urlNewTrigger = $"/buildTypes/id:{buildTypeId}/triggers";
          var response = m_caller.Post(trigger, HttpContentTypes.ApplicationJson, urlNewTrigger,
                                      HttpContentTypes.ApplicationJson);
          if (response.StatusCode != HttpStatusCode.OK) continue;

          var urlDeleteOld = $"/buildTypes/id:{buildTypeId}/triggers/{trigger.Id}";
          m_caller.Delete(urlDeleteOld);
          if (response.StatusCode == HttpStatusCode.OK)
            return true;
        }
      }
      return false;
    }

    public bool ModifSnapshotDependencies(string buildTypeId, string dependencyId, string newBt)
    {
      var urlExtractOld = $"/buildTypes/id:{buildTypeId}/snapshot-dependencies/{dependencyId}";
      var snapshot = (CustomSnapshotDependency)m_caller.GetFormat<SnapshotDependency>(urlExtractOld);
      snapshot.Id = newBt;
      snapshot.SourceBuildType.Id = newBt;

      var urlNewTrigger = $"/buildTypes/id:{buildTypeId}/snapshot-dependencies";

      var response = m_caller.Post(snapshot, HttpContentTypes.ApplicationJson, urlNewTrigger, HttpContentTypes.ApplicationJson);
      if (response.StatusCode == HttpStatusCode.OK)
      {
        var urlDeleteOld = $"/buildTypes/id:{buildTypeId}/snapshot-dependencies/{dependencyId}";
        m_caller.Delete(urlDeleteOld);
        if (response.StatusCode == HttpStatusCode.OK)
          return true;
      }

      return false;
    }

    public bool ModifArtifactDependencies(string buildTypeId, string dependencyId, string newBt)
    {
      var urlAllExtractOld = $"/buildTypes/id:{buildTypeId}/artifact-dependencies";
      var artifacts = (CustomArtifactDependencies)m_caller.GetFormat<ArtifactDependencies>(urlAllExtractOld);
      
      foreach (var artifact in artifacts.ArtifactDependency.OrderByDescending(m => m.Id))
      {
        if (dependencyId != artifact.SourceBuildType.Id) continue;

        var oldArtifactId = artifact.Id;
        artifact.SourceBuildType.Id = newBt;
        artifact.Id = null;

        var urlNewTrigger = $"/buildTypes/id:{buildTypeId}/artifact-dependencies";


        var response = m_caller.Post(artifact, HttpContentTypes.ApplicationJson, urlNewTrigger,
                                    HttpContentTypes.ApplicationJson);
        if (response.StatusCode == HttpStatusCode.OK)
        {
          var urlDeleteOld = $"/buildTypes/id:{buildTypeId}/artifact-dependencies/{oldArtifactId}";
          m_caller.Delete(urlDeleteOld);
          return response.StatusCode == HttpStatusCode.OK;
        }
      }

      return false;
    }

    public Branches GetBranchesByBuildConfigurationId(string buildTypeId,BranchLocator locator = null)
    {
      var locatorString = (locator!=null)?$"?locator={locator}":"";
      var branches =
        m_caller.Get<Branches>(
          ActionHelper.CreateFieldUrl(
            $"/buildTypes/id:{buildTypeId}/branches{locatorString}", m_fields));
      return branches;
    }

    public ArtifactDependencies GetArtifactDependencies(string buildTypeId)
    {
      var artifactDependencies =
        m_caller.Get<ArtifactDependencies>(
          ActionHelper.CreateFieldUrl(
            $"/buildTypes/id:{buildTypeId}/artifact-dependencies", m_fields));
      return artifactDependencies;
    }
    public SnapshotDependencies GetSnapshotDependencies(string buildTypeId)
    {
      var snapshotDependencies =
        m_caller.Get<SnapshotDependencies>(
          ActionHelper.CreateFieldUrl(
            $"/buildTypes/id:{buildTypeId}/snapshot-dependencies", m_fields));
      return snapshotDependencies;
    }

    public Template GetTemplate(BuildTypeLocator locator)
    {
      try
      {
        var templatedWrapper =
          m_caller.GetFormat<Template>(ActionHelper.CreateFieldUrl("/buildTypes/{0}/template", m_fields), locator);
        return templatedWrapper;
      }
      catch
      {
        return null;
      }
    }

    public Templates GetTemplates(BuildTypeLocator locator)
    {
      try
      {
        var templatedWrapper =
          m_caller.GetFormat<Templates>(ActionHelper.CreateFieldUrl("/buildTypes/{0}/templates", m_fields), locator);
        return templatedWrapper;
      }
      catch
      {
        return null;
      }
    }

    public void AttachTemplate(BuildTypeLocator locator, string templateId)
    {
      m_caller.PutFormat(templateId, HttpContentTypes.TextPlain, "/buildTypes/{0}/template", locator);
    }

    public void AttachTemplates(BuildTypeLocator locator, Templates templateList)
    {
      m_caller.PutFormat<Templates>(templateList, HttpContentTypes.ApplicationJson, HttpContentTypes.ApplicationJson, "/buildTypes/{0}/templates", locator);
    }

    public void DetachTemplate(BuildTypeLocator locator)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/template", locator);
    }

    public void DetachTemplates(BuildTypeLocator locator)
    {
      m_caller.DeleteFormat("/buildTypes/{0}/templates", locator);
    }

    #region Custom structure for copy
    internal class CustomSourceBuildType
    {
      [JsonProperty("id")]
      internal string Id { get; set; }
    }

    #region Artifact
    internal class CustomArtifactDependencies
    {
      [JsonProperty("artifact-dependency")]
      public List<CustomArtifactDependency> ArtifactDependency { get; set; }

      public static explicit operator CustomArtifactDependencies(ArtifactDependencies artifactDependencies)
      {
        var tmpArtifactDependencies = new CustomArtifactDependencies { ArtifactDependency = new List<CustomArtifactDependency>() };
        foreach (var currentArtifactDependency in artifactDependencies.ArtifactDependency)
        {
          tmpArtifactDependencies.ArtifactDependency.Add(new CustomArtifactDependency
          {
            Id = currentArtifactDependency.Id,
            Type = currentArtifactDependency.Type,
            Properties = currentArtifactDependency.Properties,
            SourceBuildType = new CustomSourceBuildType { Id = currentArtifactDependency.SourceBuildType.Id }
          });
        }
        return tmpArtifactDependencies;
      }
    }

    internal class CustomArtifactDependency
    {

      [JsonProperty("source-buildType")]
      internal CustomSourceBuildType SourceBuildType { get; set; }

      [JsonProperty("id")]
      internal string Id { get; set; }

      [JsonProperty("type")]
      internal string Type { get; set; }

      [JsonProperty("properties")]
      internal Properties Properties { get; set; }

      public static explicit operator CustomArtifactDependency(SnapshotDependency v)
      {
        throw new NotImplementedException();
      }
    }
    #endregion

    #region Snapshot
    internal class CustomSnapshotDependencies
    {
      [JsonProperty("snapshot-dependency")]
      public List<CustomSnapshotDependency> SnapshotDependency { get; set; }

      public static explicit operator CustomSnapshotDependencies(SnapshotDependencies snapshotDependencies)
      {
        var tmpSnapshotDependencies = new CustomSnapshotDependencies { SnapshotDependency = new List<CustomSnapshotDependency>() };
        foreach (var currentSnapshotDependency in snapshotDependencies.SnapshotDependency)
        {
          tmpSnapshotDependencies.SnapshotDependency.Add(new CustomSnapshotDependency
          {
            Id = currentSnapshotDependency.Id,
            Type = currentSnapshotDependency.Type,
            Properties = currentSnapshotDependency.Properties,
            SourceBuildType = new CustomSourceBuildType { Id = currentSnapshotDependency.SourceBuildType.Id }
          });
        }
        return tmpSnapshotDependencies;
      }
    }

    internal class CustomSnapshotDependency
    {

      [JsonProperty("source-buildType")]
      internal CustomSourceBuildType SourceBuildType { get; set; }

      [JsonProperty("id")]
      internal string Id { get; set; }

      [JsonProperty("type")]
      internal string Type { get; set; }

      [JsonProperty("properties")]
      internal Properties Properties { get; set; }

      public static explicit operator CustomSnapshotDependency(SnapshotDependency snapshotDependency)
      {
        return new CustomSnapshotDependency
        {
          Id = snapshotDependency.Id,
          Type = snapshotDependency.Type,
          Properties = snapshotDependency.Properties,
          SourceBuildType = new CustomSourceBuildType { Id = snapshotDependency.SourceBuildType.Id }
        };
      }
    }
    #endregion
    #endregion
  }

}