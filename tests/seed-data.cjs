/**
 * 数据库初始化种子脚本
 * 按依赖顺序创建基础数据，数据库被清空后运行此脚本
 *
 * 数据创建顺序：
 *  1. 登录获取 token
 *  2. 规格模板 (SpecTemplate)
 *  3. 配件分类 (PartCategory) - 依赖模板
 *  4. 配件 (Part) - 依赖模板、分类
 *  5. 库存流水 (StockTransaction) - 依赖配件
 *  6. 项目 (Project)
 *  7. 选型计划 (SelectionPlan) - 依赖项目、配件
 *  8. 提交选型单 (Submit) - 自动锁库+生成采购任务
 *  9. 流程定义 (WorkflowDefinition) - 依赖用户
 *
 * 运行: node tests/seed-data.cjs
 */

const API_BASE = 'http://localhost:5128/api';

let token = null;
let userId = null;

// 存储创建的各实体 ID
const ids = {
  templateMotor: null,
  templateServo: null,
  templateGearbox: null,
  categoryMotor: null,
  categoryServo: null,
  categoryDcServo: null,
  categoryGearbox: null,
  categorySensor: null,
  parts: [],               // [{id, name, category}]
  projects: [],            // [{id, name}]
  selectionPlans: [],      // [{id, name}]
};

// ==================== 辅助函数 ====================

async function api(method, path, body = null) {
  const opts = {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };
  if (body) opts.body = JSON.stringify(body);

  const res = await fetch(`${API_BASE}${path}`, opts);
  const text = await res.text();
  let data;
  try { data = JSON.parse(text); } catch { data = text; }

  if (!res.ok) {
    throw new Error(`${method} ${path} 失败 [${res.status}]: ${typeof data === 'string' ? data.substring(0, 200) : JSON.stringify(data).substring(0, 200)}`);
  }
  return data;
}

function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}

// ==================== 1. 登录 ====================

async function step1_login() {
  console.log('\n========== [1/9] 登录 ==========');
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' }),
  });
  const data = await res.json();
  if (!data.token) throw new Error('登录失败: ' + JSON.stringify(data));

  token = data.token;
  userId = data.user?.id;
  if (!userId) {
    // 如果登录没有返回 id，从用户列表获取
    const users = await api('GET', '/users');
    const admin = (users.data || users || []).find(u => u.username === 'admin');
    userId = admin?.id || 'admin';
  }
  console.log(`✓ 登录成功 userId: ${userId}`);
}

// ==================== 2. 规格模板 ====================

async function step2_templates() {
  console.log('\n========== [2/9] 创建规格模板 ==========');

  const tpl1 = await api('POST', '/templates', {
    category: '电机通用模板',
    paramDefs: [
      { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: true },
      { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: true },
      { key: 'current', label: '电流', unit: 'A', dataType: 'number', options: [], required: false },
      { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false },
    ],
  });
  ids.templateMotor = tpl1.id;
  console.log(`✓ 电机通用模板: ${tpl1.id}`);

  const tpl2 = await api('POST', '/templates', {
    category: '直流伺服专用模板',
    paramDefs: [
      { key: 'power', label: '功率', unit: 'W', dataType: 'number', options: [], required: true },
      { key: 'torque', label: '额定扭矩', unit: 'N·m', dataType: 'number', options: [], required: false },
      { key: 'speed', label: '额定转速', unit: 'rpm', dataType: 'number', options: [], required: false },
      { key: 'encoder', label: '编码器', unit: '', dataType: 'select', options: ['17bit', '23bit'], required: false },
      { key: 'brake', label: '抱闸', unit: '', dataType: 'boolean', options: [], required: false },
    ],
  });
  ids.templateServo = tpl2.id;
  console.log(`✓ 直流伺服专用模板: ${tpl2.id}`);

  const tpl3 = await api('POST', '/templates', {
    category: '减速机通用模板',
    paramDefs: [
      { key: 'ratio', label: '减速比', unit: '', dataType: 'number', options: [], required: true },
      { key: 'backlash', label: '背隙', unit: 'arcmin', dataType: 'number', options: [], required: false },
      { key: 'ratedTorque', label: '额定扭矩', unit: 'N·m', dataType: 'number', options: [], required: false },
    ],
  });
  ids.templateGearbox = tpl3.id;
  console.log(`✓ 减速机通用模板: ${tpl3.id}`);
}

// ==================== 3. 配件分类 ====================

async function step3_categories() {
  console.log('\n========== [3/9] 创建配件分类 ==========');

  const cat1 = await api('POST', '/part-categories', {
    name: '电机',
    path: '电机',
    parentId: null,
    specTemplateId: ids.templateMotor,
    sortOrder: 0,
  });
  ids.categoryMotor = cat1.id;
  console.log(`✓ 电机: ${cat1.id}`);

  const cat2 = await api('POST', '/part-categories', {
    name: '伺服电机',
    path: '电机/伺服电机',
    parentId: cat1.id,
    specTemplateId: null,
    sortOrder: 0,
  });
  ids.categoryServo = cat2.id;
  console.log(`✓ 伺服电机: ${cat2.id}`);

  const cat3 = await api('POST', '/part-categories', {
    name: '直流伺服',
    path: '电机/伺服电机/直流伺服',
    parentId: cat2.id,
    specTemplateId: ids.templateServo,
    sortOrder: 0,
  });
  ids.categoryDcServo = cat3.id;
  console.log(`✓ 直流伺服: ${cat3.id}`);

  const cat4 = await api('POST', '/part-categories', {
    name: '减速机',
    path: '减速机',
    parentId: null,
    specTemplateId: ids.templateGearbox,
    sortOrder: 1,
  });
  ids.categoryGearbox = cat4.id;
  console.log(`✓ 减速机: ${cat4.id}`);

  const cat5 = await api('POST', '/part-categories', {
    name: '传感器',
    path: '传感器',
    parentId: null,
    specTemplateId: null,
    sortOrder: 2,
  });
  ids.categorySensor = cat5.id;
  console.log(`✓ 传感器: ${cat5.id}`);
}

// ==================== 4. 配件 ====================

async function step4_parts() {
  console.log('\n========== [4/9] 创建配件 ==========');

  const partsData = [
    {
      name: '台达 400W 直流伺服电机',
      model: 'ECMA-C20604RS',
      description: '400W 高性能直流伺服电机，适用于精密定位',
      manufacturer: '台达 (Delta)',
      brand: '台达',
      category: '电机/伺服电机/直流伺服',
      tags: ['伺服电机', '400W', '台达'],
      specTemplateId: ids.templateServo,
      specs: [
        { key: 'power', label: '功率', value: '400', unit: 'W' },
        { key: 'torque', label: '额定扭矩', value: '1.27', unit: 'N·m' },
        { key: 'speed', label: '额定转速', value: '3000', unit: 'rpm' },
        { key: 'encoder', label: '编码器', value: '23bit', unit: '' },
        { key: 'brake', label: '抱闸', value: 'true', unit: '' },
      ],
    },
    {
      name: '安川 750W 伺服电机',
      model: 'SGM7G-09AFC61',
      description: '750W 中惯量伺服电机',
      manufacturer: '安川 (Yaskawa)',
      brand: '安川',
      category: '电机/伺服电机/直流伺服',
      tags: ['伺服电机', '750W', '安川'],
      specTemplateId: ids.templateServo,
      specs: [
        { key: 'power', label: '功率', value: '750', unit: 'W' },
        { key: 'torque', label: '额定扭矩', value: '2.39', unit: 'N·m' },
        { key: 'speed', label: '额定转速', value: '3000', unit: 'rpm' },
        { key: 'encoder', label: '编码器', value: '17bit', unit: '' },
        { key: 'brake', label: '抱闸', value: 'false', unit: '' },
      ],
    },
    {
      name: '西门子 1.5kW 异步电机',
      model: '1LE1001-0DA22-2AA4',
      description: '1.5kW 高效异步电动机，IE3能效等级',
      manufacturer: '西门子 (Siemens)',
      brand: '西门子',
      category: '电机',
      tags: ['异步电机', '1.5kW', '西门子', 'IE3'],
      specTemplateId: ids.templateMotor,
      specs: [
        { key: 'power', label: '功率', value: '1.5', unit: 'kW' },
        { key: 'voltage', label: '电压', value: '380', unit: 'V' },
        { key: 'current', label: '电流', value: '3.5', unit: 'A' },
        { key: 'efficiency', label: '效率等级', value: 'IE3', unit: '' },
      ],
    },
    {
      name: '新宝 行星减速机 PL60',
      model: 'PL60-010',
      description: '行星减速机，适配200-400W伺服电机',
      manufacturer: '新宝 (SHIMPO)',
      brand: '新宝',
      category: '减速机',
      tags: ['行星减速机', 'PL60', '新宝'],
      specTemplateId: ids.templateGearbox,
      specs: [
        { key: 'ratio', label: '减速比', value: '10', unit: '' },
        { key: 'backlash', label: '背隙', value: '8', unit: 'arcmin' },
        { key: 'ratedTorque', label: '额定扭矩', value: '20', unit: 'N·m' },
      ],
    },
    {
      name: '新宝 行星减速机 PL80',
      model: 'PL80-020',
      description: '行星减速机，适配750W-1kW伺服电机',
      manufacturer: '新宝 (SHIMPO)',
      brand: '新宝',
      category: '减速机',
      tags: ['行星减速机', 'PL80', '新宝'],
      specTemplateId: ids.templateGearbox,
      specs: [
        { key: 'ratio', label: '减速比', value: '20', unit: '' },
        { key: 'backlash', label: '背隙', value: '8', unit: 'arcmin' },
        { key: 'ratedTorque', label: '额定扭矩', value: '40', unit: 'N·m' },
      ],
    },
    {
      name: '欧姆龙 光电传感器 E3Z',
      model: 'E3Z-D61',
      description: '光电传感器，扩散反射型，检测距离1m',
      manufacturer: '欧姆龙 (Omron)',
      brand: '欧姆龙',
      category: '传感器',
      tags: ['光电传感器', '欧姆龙'],
      specTemplateId: null,
      specs: [],
    },
    {
      name: '基恩士 激光传感器 IL-100',
      model: 'IL-100',
      description: 'CMOS激光传感器，高精度位移检测',
      manufacturer: '基恩士 (KEYENCE)',
      brand: '基恩士',
      category: '传感器',
      tags: ['激光传感器', '基恩士'],
      specTemplateId: null,
      specs: [],
    },
  ];

  for (const part of partsData) {
    const created = await api('POST', '/parts', part);
    ids.parts.push({ id: created.id, name: created.name, category: part.category });
    console.log(`✓ ${part.name} (${part.model}): ${created.id}`);
  }

  console.log(`  共创建 ${ids.parts.length} 个配件`);
}

// ==================== 5. 库存流水 ====================

async function step5_stock() {
  console.log('\n========== [5/9] 初始化库存 ==========');

  const inboundOps = [
    { partIdx: 0, qty: 50 },   // 台达400W: 入库50
    { partIdx: 1, qty: 30 },   // 安川750W: 入库30
    { partIdx: 2, qty: 20 },   // 西门子1.5kW: 入库20
    { partIdx: 3, qty: 40 },   // 新宝PL60: 入库40
    { partIdx: 4, qty: 25 },   // 新宝PL80: 入库25
    { partIdx: 5, qty: 100 },  // 欧姆龙E3Z: 入库100
    { partIdx: 6, qty: 15 },   // 基恩士IL-100: 入库15
  ];

  for (const op of inboundOps) {
    const part = ids.parts[op.partIdx];
    await api('POST', '/stock/inbound', {
      partId: part.id,
      quantity: op.qty,
      sourceType: 'Manual',
      note: '种子脚本初始化库存',
    });
    console.log(`✓ ${part.name}: +${op.qty} 入库`);
  }
}

// ==================== 6. 项目 ====================

async function step6_projects() {
  console.log('\n========== [6/9] 创建项目 ==========');

  const projectsData = [
    {
      name: '自动化装配线改造项目',
      type: 'project',
      status: 'active',
      description: '对现有装配线进行自动化升级改造',
      ownerId: userId,
    },
    {
      name: '激光打标机开发项目',
      type: 'project',
      status: 'active',
      description: '新一代高精度激光打标机整机开发',
      ownerId: userId,
    },
    {
      name: '码垛机器人集成项目',
      type: 'project',
      status: 'planning',
      description: '码垛机器人的选型和集成方案设计',
      ownerId: userId,
    },
  ];

  for (const proj of projectsData) {
    const created = await api('POST', '/projects', proj);
    ids.projects.push({ id: created.id, name: created.name });
    console.log(`✓ ${proj.name}: ${created.id}`);
    // 等待工作区初始化
    await sleep(1500);
  }
}

// ==================== 7. 选型计划 ====================

async function step7_selections() {
  console.log('\n========== [7/9] 创建选型计划 ==========');

  const selectionsData = [
    {
      projectIdx: 0,
      name: '装配线伺服驱动选型',
      items: [
        { partIdx: 0, qty: 5 },   // 台达400W x 5
        { partIdx: 1, qty: 3 },   // 安川750W x 3
        { partIdx: 3, qty: 5 },   // 新宝PL60 x 5
      ],
    },
    {
      projectIdx: 0,
      name: '装配线传感器选型',
      items: [
        { partIdx: 5, qty: 20 },  // 欧姆龙E3Z x 20
        { partIdx: 6, qty: 5 },   // 基恩士IL-100 x 5
      ],
    },
    {
      projectIdx: 1,
      name: '激光打标机运动系统',
      items: [
        { partIdx: 2, qty: 2 },   // 西门子1.5kW x 2
        { partIdx: 4, qty: 2 },   // 新宝PL80 x 2
      ],
    },
    {
      projectIdx: 2,
      name: '码垛机器人驱动方案',
      items: [
        { partIdx: 1, qty: 6 },   // 安川750W x 6
        { partIdx: 2, qty: 4 },   // 西门子1.5kW x 4
        { partIdx: 4, qty: 3 },   // 新宝PL80 x 3
        { partIdx: 5, qty: 10 },  // 欧姆龙E3Z x 10
      ],
    },
  ];

  for (const sel of selectionsData) {
    const project = ids.projects[sel.projectIdx];

    // 创建选型单（草稿状态，带配件列表）
    const items = sel.items.map(item => {
      const part = ids.parts[item.partIdx];
      return {
        selectedPartId: part.id,
        partName: part.name,
        category: part.category,
        requiredQty: item.qty,
        lockedQty: 0,
      };
    });

    const created = await api('POST', '/selections', {
      name: sel.name,
      projectId: project.id,
      items: items,
    });
    console.log(`✓ ${sel.name} (${project.name}): ${created.id}  (${items.length} 个配件)`);

    ids.selectionPlans.push({ id: created.id, name: sel.name, projectId: project.id });
  }
}

// ==================== 8. 提交选型单（自动锁库+生成采购任务）====================

async function step8_submitSelections() {
  console.log('\n========== [8/9] 提交选型单 ==========');

  // 提交第一个选型单: 库存足够时直接锁定，库存不够时自动生成采购任务
  const sel = ids.selectionPlans[0]; // "装配线伺服驱动选型"
  console.log(`提交: ${sel.name}...`);
  const result = await api('POST', `/selections/${sel.id}/submit`);
  console.log(`✓ ${sel.name} 已提交 (锁库完成)`);
  console.log(`  结果摘要: ${JSON.stringify(result).substring(0, 300)}`);

  // 提交第二个选型单
  const sel2 = ids.selectionPlans[1]; // "装配线传感器选型"
  console.log(`提交: ${sel2.name}...`);
  const result2 = await api('POST', `/selections/${sel2.id}/submit`);
  console.log(`✓ ${sel2.name} 已提交`);
  if (result2.message || result2.length || typeof result2 === 'object') {
    console.log(`  结果摘要: ${JSON.stringify(result2).substring(0, 300)}`);
  }
}

// ==================== 9. 流程定义 ====================

async function step9_workflows() {
  console.log('\n========== [9/9] 创建流程定义 ==========');

  // 单步审批流程
  const def1 = await api('POST', '/workflows/definitions', {
    name: '配件选型审批流程（单步）',
    description: '技术方案配件选型单的简单审批',
    category: '选型审批',
    entityType: '',
    version: 1,
    isActive: true,
    startConfig: {
      requireEntity: false,
      entityType: 'Project',
      requireFiles: false,
      minFileCount: 0,
      maxFileCount: 0,
      allowedFileTypes: [],
      formFields: [],
    },
    nodes: [
      {
        id: 'node_start',
        name: '开始',
        nodeType: 'Start',
        nextNodes: ['node_approval'],
        formFields: [
          {
            id: 'field_reason',
            label: '选型说明',
            key: 'reason',
            fieldType: 'textarea',
            placeholder: '请填写选型理由',
            required: true,
            options: [],
          },
        ],
      },
      {
        id: 'node_approval',
        name: '技术审批',
        nodeType: 'SingleApproval',
        approvers: [userId],
        timeoutMinutes: 120,
        nextNodes: ['node_end'],
        formFields: [
          {
            id: 'field_opinion',
            label: '审批意见',
            key: 'opinion',
            fieldType: 'textarea',
            placeholder: '请填写审批意见',
            required: true,
            options: [],
          },
        ],
      },
      {
        id: 'node_end',
        name: '结束',
        nodeType: 'End',
        nextNodes: [],
      },
    ],
  });
  console.log(`✓ ${def1.name}: ${def1.id}`);

  // 两步审批流程
  const def2 = await api('POST', '/workflows/definitions', {
    name: '采购申请审批流程（两步）',
    description: '采购申请需要主管和经理两级审批',
    category: '采购审批',
    entityType: '',
    version: 1,
    isActive: true,
    startConfig: {
      requireEntity: false,
      entityType: '',
      requireFiles: false,
      minFileCount: 0,
      maxFileCount: 0,
      allowedFileTypes: [],
      formFields: [],
    },
    nodes: [
      {
        id: 'node_start',
        name: '开始',
        nodeType: 'Start',
        nextNodes: ['node_approval1'],
        formFields: [
          {
            id: 'field_amount',
            label: '采购金额',
            key: 'amount',
            fieldType: 'number',
            placeholder: '请输入采购金额',
            required: true,
            options: [],
          },
          {
            id: 'field_reason',
            label: '采购原因',
            key: 'purchase_reason',
            fieldType: 'textarea',
            placeholder: '请说明采购原因',
            required: true,
            options: [],
          },
        ],
      },
      {
        id: 'node_approval1',
        name: '主管审批',
        nodeType: 'SingleApproval',
        approvers: [userId],
        timeoutMinutes: 60,
        nextNodes: ['node_approval2'],
        formFields: [
          {
            id: 'field_dept_opinion',
            label: '部门意见',
            key: 'dept_opinion',
            fieldType: 'textarea',
            placeholder: '请填写部门审批意见',
            required: false,
            options: [],
          },
        ],
      },
      {
        id: 'node_approval2',
        name: '经理审批',
        nodeType: 'SingleApproval',
        approvers: [userId],
        timeoutMinutes: 60,
        nextNodes: ['node_end'],
        formFields: [
          {
            id: 'field_mgr_opinion',
            label: '经理意见',
            key: 'mgr_opinion',
            fieldType: 'select',
            placeholder: '请选择审批结果',
            required: true,
            options: ['同意', '退回', '需修改'],
          },
        ],
      },
      {
        id: 'node_end',
        name: '结束',
        nodeType: 'End',
        nextNodes: [],
      },
    ],
  });
  console.log(`✓ ${def2.name}: ${def2.id}`);
}

// ==================== 主流程 ====================

async function main() {
  console.log('========================================');
  console.log('   PartSelectionSystem 数据库初始化种子脚本');
  console.log('========================================');
  console.log(`  目标: ${API_BASE}`);
  console.log(`  时间: ${new Date().toLocaleString()}`);
  console.log('========================================');

  try {
    await step1_login();            // 1. 登录
    await step2_templates();        // 2. 规格模板
    await step3_categories();       // 3. 配件分类
    await step4_parts();            // 4. 配件
    await step5_stock();            // 5. 库存
    await step6_projects();         // 6. 项目
    await step7_selections();       // 7. 选型计划
    await step8_submitSelections(); // 8. 提交选型单
    await step9_workflows();        // 9. 流程定义

    console.log('\n========================================');
    console.log('  初始化完成！');
    console.log('========================================');
    console.log(`  规格模板: 3 个 (电机通用/直流伺服专用/减速机通用)`);
    console.log(`  配件分类: 5 个 (电机/伺服电机/直流伺服/减速机/传感器)`);
    console.log(`  配件:     ${ids.parts.length} 个`);
    console.log(`  项目:     ${ids.projects.length} 个`);
    console.log(`  选型计划: ${ids.selectionPlans.length} 个 (已提交前2个)`);
    console.log(`  流程定义: 2 个 (单步审批/两步审批)`);
    console.log('========================================');

  } catch (err) {
    console.error('\n✗ 初始化失败:', err.message);
    process.exit(1);
  }
}

main();
