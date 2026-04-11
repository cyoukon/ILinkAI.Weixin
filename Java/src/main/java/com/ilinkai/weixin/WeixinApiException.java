package com.ilinkai.weixin;

/** 微信API异常 */
public class WeixinApiException extends Exception {
    private final int statusCode;

    public WeixinApiException(String message, int statusCode) {
        super(message);
        this.statusCode = statusCode;
    }

    public int getStatusCode() { return statusCode; }
}
