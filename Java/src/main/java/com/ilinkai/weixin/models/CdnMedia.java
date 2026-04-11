package com.ilinkai.weixin.models;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

/** CDN媒体引用信息 */
@JsonInclude(JsonInclude.Include.NON_NULL)
public class CdnMedia {
    @JsonProperty("encrypt_query_param")
    private String encryptQueryParam;

    @JsonProperty("aes_key")
    private String aesKey;

    @JsonProperty("encrypt_type")
    private Integer encryptType;

    public String getEncryptQueryParam() { return encryptQueryParam; }
    public void setEncryptQueryParam(String v) { this.encryptQueryParam = v; }
    public String getAesKey() { return aesKey; }
    public void setAesKey(String v) { this.aesKey = v; }
    public Integer getEncryptType() { return encryptType; }
    public void setEncryptType(Integer v) { this.encryptType = v; }
}
