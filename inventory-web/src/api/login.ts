/**
 * 登录接口
 */
import request from '/@/utils/request';

// 登录接口

export function getLogin(data: any) {
  return request({
    url: '/api/auth/login',
    method: 'post',
    data,
  });
}