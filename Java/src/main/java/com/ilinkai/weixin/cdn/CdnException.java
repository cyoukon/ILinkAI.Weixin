package com.ilinkai.weixin.cdn;

/** CDN操作异常 */
public class CdnException extends Exception {
    private final int statusCode;

    public CdnException(String message) { this(message, 0); }

    public CdnException(String message, int statusCode) {
        super(message);
        this.statusCode = statusCode;
    }

    public int getStatusCode() { return statusCode; }
}
