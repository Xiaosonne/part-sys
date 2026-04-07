import { createRouter, createWebHistory } from 'vue-router'
import { getToken } from '@/utils/auth'
import Login from '@/views/Login.vue'
import Layout from '@/views/Layout.vue'
import Stock from '@/views/Stock.vue'
import Transactions from '@/views/Transactions.vue'
import Projects from '@/views/Projects.vue'
import Selections from '@/views/Selections.vue'
import PurchaseTasks from '@/views/PurchaseTasks.vue'
import Users from '@/views/Users.vue'
import WorkflowDesigner from '@/views/WorkflowDesigner.vue'
import PendingTasks from '@/views/PendingTasks.vue'
import MyWorkflows from '@/views/MyWorkflows.vue'
import StartWorkflow from '@/views/StartWorkflow.vue'
import WorkflowDetail from '@/views/WorkflowDetail.vue'
import SpecTemplates from '@/views/SpecTemplates.vue'
import PartManagement from '@/views/PartManagement.vue'

const routes = [
  {
    path: '/login',
    component: Login
  },
  {
    path: '/',
    component: Layout,
    children: [
      { path: 'parts', redirect: '/parts/manage' },
      { path: 'parts/manage', component: PartManagement, name: 'PartManagement' },
      { path: 'stock', component: Stock },
      { path: 'transactions', component: Transactions },
      { path: 'projects', component: Projects, name: 'Projects' },
      { path: 'selections', component: Selections },
      { path: 'purchase-tasks', component: PurchaseTasks },
      { path: 'users', component: Users },
      { path: 'workflows/start', component: StartWorkflow, name: 'StartWorkflow' },
      { path: 'workflows/designer', component: WorkflowDesigner, name: 'WorkflowDesigner' },
      { path: 'workflows/pending', component: PendingTasks, name: 'PendingTasks' },
      { path: 'workflows/my', component: MyWorkflows, name: 'MyWorkflows' },
      { path: 'workflows/detail/:id', component: WorkflowDetail, name: 'WorkflowDetail' },
      { path: 'spec-templates', component: SpecTemplates, name: 'SpecTemplates' }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  if (to.path !== '/login' && !getToken()) {
    next('/login')
  } else {
    next()
  }
})

export default router
