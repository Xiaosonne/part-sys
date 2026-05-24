<template>
  <div class="login-container" :style="containerStyle">
    <div class="login-layout">
      <div class="login-right">
        <el-card class="login-card" shadow="hover">
          <div class="logo-area">
            <div class="platform-name">欢迎登录</div>
            <div class="company-name">Warehouse Management System</div>
          </div>

          <el-form
            ref="loginFormRef"
            :model="loginForm"
            class="login-form"
            label-position="top"
          >
            <el-form-item prop="username">
              <el-input v-model="loginForm.username" placeholder="账号">
                <template #prefix>
                  <el-icon><User /></el-icon>
                </template>
              </el-input>
            </el-form-item>

            <el-form-item prop="password">
              <el-input v-model="loginForm.password" type="password" placeholder="密码" show-password>
                <template #prefix>
                  <el-icon><Lock /></el-icon>
                </template>
              </el-input>
            </el-form-item>

            <el-form-item class="submit-row">
              <el-button type="primary" class="login-btn" @click="handleLogin">
                登录
              </el-button>
            </el-form-item>
          </el-form>

          <div class="copyright">© 2026 物流仓储管理平台</div>
        </el-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { ElForm, ElFormItem, ElInput, ElButton, ElCard } from 'element-plus';
import { User, Lock } from '@element-plus/icons-vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
const route = useRoute();
const router = useRouter();
import { Session } from '/@/utils/storage';
import Cookies from 'js-cookie';
import { initFrontEndControlRoutes } from '/@/router/frontEnd';
import backgroundUrl from '/@/assets/background.jpg';
import { getLogin } from '/@/api/login';
// 登录表单数据
const loginForm = ref({
  username: '',
  password: '',
});
const loginFormRef = ref<InstanceType<typeof ElForm>>();

const containerStyle = {
  backgroundImage: `url(${backgroundUrl})`,
  backgroundPosition: 'center',
  backgroundSize: 'cover',
  backgroundRepeat: 'no-repeat',
};

type InventorySystemLoginResponse = {
  token: string;
  user?: {
    id?: string;
    username?: string;
    displayName?: string;
    role?: string;
  };
};

const loginInventorySystem = async (username: string, password: string): Promise<InventorySystemLoginResponse> => {
  try {
    const data = (await getLogin({ username, password })) as InventorySystemLoginResponse;
    if (!data?.token) throw new Error('登录失败');
    return data;
  } catch (e: any) {
    const message = e?.response?.data?.message || e?.message || '登录失败';
    throw new Error(message);
  }
};

// 登录处理函数
const handleLogin = () => {
  loginFormRef.value?.validate(async (valid) => {
    if (valid) {
      const username = loginForm.value.username?.trim();
      const password = loginForm.value.password;
      if (!username || !password) {
        ElMessage.warning('请输入账号和密码');
        return;
      }
      try {
        const res = await loginInventorySystem(username, password);
        if (!res?.token) throw new Error('登录失败 ');
        Session.set('token', `Bearer ${res.token}`);
        const user = (res as any).user ?? (res as any);
        const userName = user?.username || username;
        const displayName = user?.displayName || userName;
        const role = user?.role || 'common';
        const photo = user?.photo || user?.avatar || user?.image || '';
        Cookies.set('userName', userName);
        Session.set('userInfo', {
          id: user?.id,
          userName,
          displayName,
          role,
          photo,
          time: Date.now(),
          roles: [role],
        });
        const isNoPower = await initFrontEndControlRoutes();
        signInSuccess(isNoPower);
        ElMessage.success('登录成功');
      } catch (e: any) {
        ElMessage.error(e?.message || '登录失败');
      }
    }
  });
};
const signInSuccess = (isNoPower: boolean | undefined) => {
	if (isNoPower) {
		ElMessage.warning('抱歉，您没有登录权限');
		Session.clear();
	} else {
		if (route.query?.redirect) {
			router.push({
				path: <string>route.query?.redirect,
				query: Object.keys(<string>route.query?.params).length > 0 ? JSON.parse(<string>route.query?.params) : '',
			});
		} else {
			router.push('/system/menu');
		}
	}
};
</script>

<style scoped lang="scss">
.login-container {
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding: 0 150px;
  overflow: hidden;
}

.login-layout {
  display: flex;
  align-items: center;
}

.login-right {
  width: 440px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.login-card {
  width: 100%;
  border-radius: 16px;
  padding: 30px 34px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(226, 232, 240, 0.9);
  backdrop-filter: blur(14px);
  box-shadow:
    0 20px 45px rgba(2, 132, 199, 0.18),
    0 8px 18px rgba(15, 23, 42, 0.08);
}

.logo-area {
  text-align: center;
  margin-bottom: 22px;
}

.platform-name {
  font-size: 20px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: 0.4px;
}

.company-name {
  margin-top: 6px;
  font-size: 13px;
  color: rgba(71, 85, 105, 0.9);
}

.login-form {
  width: 100%;

  :deep(.el-form-item) {
    margin-bottom: 16px;
  }

  :deep(.el-form-item__label) {
    display: none;
  }

  :deep(.el-input__wrapper) {
    border-radius: 10px;
    background: rgba(255, 255, 255, 0.96);
    box-shadow: 0 0 0 1px rgba(148, 163, 184, 0.35) inset;
    transition: box-shadow 160ms ease, background-color 160ms ease;
  }

  :deep(.el-input__wrapper:hover) {
    box-shadow:
      0 0 0 1px rgba(14, 165, 233, 0.25) inset,
      0 0 0 4px rgba(56, 189, 248, 0.12);
  }

  :deep(.el-input__wrapper.is-focus) {
    box-shadow:
      0 0 0 1px rgba(14, 165, 233, 0.35) inset,
      0 0 0 5px rgba(14, 165, 233, 0.18);
  }

  :deep(.el-input__inner) {
    height: 42px;
  }
}

.submit-row {
  margin-top: 6px;
}

.login-btn {
  width: 100%;
  height: 42px;
  border-radius: 10px;
  background: linear-gradient(135deg, #38bdf8 0%, #2563eb 100%);
  border: 0;
  font-size: 16px;
  font-weight: 600;
  letter-spacing: 1px;
  box-shadow:
    0 14px 22px rgba(37, 99, 235, 0.18),
    0 10px 16px rgba(14, 165, 233, 0.14);
  transition: transform 160ms ease, box-shadow 160ms ease, filter 160ms ease;
}

.login-btn:hover {
  filter: brightness(1.03);
  transform: translateY(-1px);
  box-shadow:
    0 18px 28px rgba(37, 99, 235, 0.26),
    0 12px 18px rgba(14, 165, 233, 0.2);
}

.login-btn:active {
  transform: translateY(0);
}

.copyright {
  text-align: center;
  font-size: 12px;
  color: rgba(100, 116, 139, 0.9);
  margin-top: 18px;
}

@media (max-width: 480px) {
  .login-container { justify-content: center; padding: 0 16px; }
  .login-right { width: 100%; }
}

@media (max-width: 980px) {
  .login-right {
    width: 100%;
  }
  .login-container { justify-content: center; padding: 0 24px; }
}
</style>
