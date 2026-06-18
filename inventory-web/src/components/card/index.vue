<template>
  <div class="card-box" :style="{ height: height || '400px' }">
    <div class="title-box">
      <div class="title"> <span></span> {{ title }}</div>
      <div class="opeate">
        <slot name="opeate"></slot>
      </div>
    </div>
    <div class="content">
      <slot name="content"></slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { ElMessage, ElMessageBox } from "element-plus";

/*----------------------------变量--------------------------------- */
const props = defineProps<{
	title?: string;
	height?: string | number;
}>();

/*----------------------------方法--------------------------------- */

</script>

<style lang="scss" scoped >
.card-box{
	width: 100%;
	height: 100%;
	border-radius: 4px;
	// border: 1px solid var(--el-border-color-light, #ebeef5);
	background-color: rgba(242, 242, 242, 0.5);
	overflow: hidden;
  padding: 12px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}
.title-box{
  margin-bottom: 12px;
  font-size: 14px;
  font-weight: bold;
  color: var(--el-text-color-primary);
  display: flex;
  justify-content: space-between;
  .title{
    display: flex;
    align-items: center;
  }
  span{
    display: inline-block;
    width: 5px;
    height: 20px;
    background-color: #02a7f0;
    margin-right: 8px;
    // border-radius: 50%;
  }
}
.content{
  height: calc(100% - 30px);
}
</style>