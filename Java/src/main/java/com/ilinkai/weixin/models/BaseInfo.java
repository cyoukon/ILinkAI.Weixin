package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

/** 基础信息，附加到每个CGI请求 */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class BaseInfo {
    @JsonProperty("channel_version")
    private String channelVersion;

    public String getChannelVersion() { return channelVersion; }
    public void setChannelVersion(String channelVersion) { this.channelVersion = channelVersion; }
}
