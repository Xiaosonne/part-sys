/**
 * Part System Test Data Seeder
 *
 * Creates test data for Part, PartCategory, and SpecTemplate.
 * Uses direct MongoDB connection for reliable data creation.
 *
 * Usage: node seed-parts.js
 *
 * MongoDB: mongodb://admin:ganwei.123@211.159.151.178:21117/inventory_db
 */

const { MongoClient, ObjectId } = require('mongodb')

const MONGO_URI = 'mongodb://admin:ganwei.123@211.159.151.178:21117'
const DB_NAME = 'inventory_db'

async function seed() {
  const client = new MongoClient(MONGO_URI)

  try {
    await client.connect()
    console.log('Connected to MongoDB')

    const db = client.db(DB_NAME)

    // Clear existing data
    console.log('Clearing existing data...')
    await db.collection('spec_templates').deleteMany({})
    await db.collection('part_categories').deleteMany({})
    await db.collection('parts').deleteMany({})

    // ============================================
    // Step 1: Create SpecTemplates
    // ============================================
    console.log('\nCreating SpecTemplates...')

    const tplMotorId = new ObjectId()
    const tplSensorId = new ObjectId()
    const tplControllerId = new ObjectId()

    const templates = [
      {
        _id: tplMotorId,
        Category: '电机',
        ParamDefs: [
          { Key: 'power', Label: '功率', Unit: 'kW', DataType: 'number', Required: false },
          { Key: 'voltage', Label: '电压', Unit: 'V', DataType: 'number', Required: false },
          { Key: 'efficiency', Label: '效率等级', DataType: 'select', Options: ['IE1', 'IE2', 'IE3', 'IE4'], Required: false },
          { Key: 'ipRating', Label: '防护等级', DataType: 'select', Options: ['IP65', 'IP66', 'IP67'], Required: false }
        ]
      },
      {
        _id: tplSensorId,
        Category: '传感器',
        ParamDefs: [
          { Key: 'range', Label: '测量范围', DataType: 'string', Required: false },
          { Key: 'accuracy', Label: '精度', Unit: '%', DataType: 'number', Required: false },
          { Key: 'output', Label: '输出类型', DataType: 'select', Options: ['4-20mA', '0-10V', 'RS485', 'Modbus'], Required: false },
          { Key: 'explosionProof', Label: '防爆', DataType: 'boolean', Required: false }
        ]
      },
      {
        _id: tplControllerId,
        Category: '控制器',
        ParamDefs: [
          { Key: 'channels', Label: '通道数', DataType: 'number', Required: false },
          { Key: 'comm', Label: '通信方式', DataType: 'select', Options: ['CAN', 'RS485', 'Ethernet'], Required: false },
          { Key: 'supplyVoltage', Label: '供电电压', Unit: 'V', DataType: 'number', Required: false },
          { Key: 'language', Label: '编程语言', DataType: 'select', Options: ['C', 'Python', 'Lua', 'Blockly'], Required: false }
        ]
      }
    ]

    await db.collection('spec_templates').insertMany(templates)
    console.log(`Created ${templates.length} templates`)

    // ============================================
    // Step 2: Create PartCategories (Multi-level)
    // ============================================
    console.log('\nCreating PartCategories...')

    // Helper to create category
    const createCategory = async (name, path, parentId, specTemplateId) => {
      const cat = {
        _id: new ObjectId(),
        Name: name,
        Path: path,
        ParentId: parentId || null,
        SpecTemplateId: specTemplateId || null,
        SortOrder: 0,
        CreatedAt: new Date()
      }
      await db.collection('part_categories').insertOne(cat)
      return cat
    }

    // Level 1: 电子元器件
    const catElectronics = await createCategory('电子元器件', '电子元器件', null, null)

    // Level 2: 微控制器
    const catMicrocontroller = await createCategory('微控制器', '电子元器件/微控制器', catElectronics._id, null)

    // Level 3: ARM, ESP32
    const catArm = await createCategory('ARM', '电子元器件/微控制器/ARM', catMicrocontroller._id, tplControllerId)
    const catEsp32 = await createCategory('ESP32', '电子元器件/微控制器/ESP32', catMicrocontroller._id, tplControllerId)

    // Level 2: 传感器
    const catSensors = await createCategory('传感器', '电子元器件/传感器', catElectronics._id, null)

    // Level 3: 温度传感器
    const catTempSensor = await createCategory('温度传感器', '电子元器件/传感器/温度传感器', catSensors._id, tplSensorId)

    // Level 1: 电机
    const catMotor = await createCategory('电机', '电机', null, null)

    // Level 2: 伺服电机
    const catServoMotor = await createCategory('伺服电机', '电机/伺服电机', catMotor._id, null)

    // Level 3: 直流伺服
    const catDcServo = await createCategory('直流伺服', '电机/伺服电机/直流伺服', catServoMotor._id, tplMotorId)

    // Level 2: 步进电机
    const catStepper = await createCategory('步进电机', '电机/步进电机', catMotor._id, null)

    // Level 3: 两相步进
    const catTwoPhase = await createCategory('两相步进', '电机/步进电机/两相步进', catStepper._id, tplMotorId)

    console.log('Created category hierarchy')

    // ============================================
    // Step 3: Create Parts
    // ============================================
    console.log('\nCreating Parts...')

    const now = new Date()

    const parts = [
      // ARM Controllers
      {
        _id: new ObjectId(),
        Name: 'ARM Cortex-M4 开发板',
        Model: 'STM32F407',
        Description: '高性能ARM Cortex-M4微控制器开发板',
        Manufacturer: 'STMicroelectronics',
        Brand: 'STM32',
        Category: '电子元器件/微控制器/ARM',
        Tags: ['ARM', 'Cortex-M4', '开发板'],
        SpecTemplateId: tplControllerId.toString(),
        Specs: [
          { Key: 'channels', Label: '通道数', Value: '8', Unit: '路' },
          { Key: 'comm', Label: '通信方式', Value: 'CAN', Unit: '' },
          { Key: 'supplyVoltage', Label: '供电电压', Value: '3.3', Unit: 'V' },
          { Key: 'language', Label: '编程语言', Value: 'C', Unit: '' }
        ],
        TotalQty: 100,
        AvailableQty: 85,
        LockedQty: 15,
        CreatedAt: now,
        UpdatedAt: now
      },
      {
        _id: new ObjectId(),
        Name: 'ARM Cortex-M3 开发板',
        Model: 'STM32F103',
        Description: '通用型ARM Cortex-M3微控制器',
        Manufacturer: 'STMicroelectronics',
        Brand: 'STM32',
        Category: '电子元器件/微控制器/ARM',
        Tags: ['ARM', 'Cortex-M3', '入门级'],
        SpecTemplateId: tplControllerId.toString(),
        Specs: [
          { Key: 'channels', Label: '通道数', Value: '4', Unit: '路' },
          { Key: 'comm', Label: '通信方式', Value: 'RS485', Unit: '' },
          { Key: 'supplyVoltage', Label: '供电电压', Value: '3.3', Unit: 'V' },
          { Key: 'language', Label: '编程语言', Value: 'C', Unit: '' }
        ],
        TotalQty: 150,
        AvailableQty: 120,
        LockedQty: 30,
        CreatedAt: now,
        UpdatedAt: now
      },
      // ESP32
      {
        _id: new ObjectId(),
        Name: 'ESP32 WiFi开发板',
        Model: 'ESP32-DevKitC',
        Description: 'ESP32双核处理器WiFi+蓝牙开发板',
        Manufacturer: 'Espressif',
        Brand: 'ESP32',
        Category: '电子元器件/微控制器/ESP32',
        Tags: ['WiFi', '蓝牙', '双核'],
        SpecTemplateId: tplControllerId.toString(),
        Specs: [
          { Key: 'channels', Label: '通道数', Value: '32', Unit: '路' },
          { Key: 'comm', Label: '通信方式', Value: 'Ethernet', Unit: '' },
          { Key: 'supplyVoltage', Label: '供电电压', Value: '3.3', Unit: 'V' },
          { Key: 'language', Label: '编程语言', Value: 'Python', Unit: '' }
        ],
        TotalQty: 200,
        AvailableQty: 180,
        LockedQty: 20,
        CreatedAt: now,
        UpdatedAt: now
      },
      // Temperature Sensors
      {
        _id: new ObjectId(),
        Name: 'PT100温度传感器',
        Model: 'PT100-1',
        Description: '工业级PT100铂电阻温度传感器',
        Manufacturer: 'Omega',
        Brand: 'Omega',
        Category: '电子元器件/传感器/温度传感器',
        Tags: ['PT100', '工业级', '铂电阻'],
        SpecTemplateId: tplSensorId.toString(),
        Specs: [
          { Key: 'range', Label: '测量范围', Value: '-50~200', Unit: '℃' },
          { Key: 'accuracy', Label: '精度', Value: '0.1', Unit: '%' },
          { Key: 'output', Label: '输出类型', Value: '4-20mA', Unit: '' },
          { Key: 'explosionProof', Label: '防爆', Value: 'true', Unit: '' }
        ],
        TotalQty: 50,
        AvailableQty: 45,
        LockedQty: 5,
        CreatedAt: now,
        UpdatedAt: now
      },
      {
        _id: new ObjectId(),
        Name: 'DS18B20数字温度传感器',
        Model: 'DS18B20',
        Description: '单总线数字温度传感器',
        Manufacturer: 'Maxim',
        Brand: 'Maxim',
        Category: '电子元器件/传感器/温度传感器',
        Tags: ['数字传感器', '单总线', '防水'],
        SpecTemplateId: tplSensorId.toString(),
        Specs: [
          { Key: 'range', Label: '测量范围', Value: '-55~125', Unit: '℃' },
          { Key: 'accuracy', Label: '精度', Value: '0.5', Unit: '%' },
          { Key: 'output', Label: '输出类型', Value: 'RS485', Unit: '' },
          { Key: 'explosionProof', Label: '防爆', Value: 'false', Unit: '' }
        ],
        TotalQty: 300,
        AvailableQty: 280,
        LockedQty: 20,
        CreatedAt: now,
        UpdatedAt: now
      },
      // DC Servo Motors
      {
        _id: new ObjectId(),
        Name: '直流伺服电机 200W',
        Model: 'DMS-200',
        Description: '200W直流伺服电机，高效率',
        Manufacturer: 'Delta',
        Brand: 'Delta',
        Category: '电机/伺服电机/直流伺服',
        Tags: ['伺服电机', '直流', '200W'],
        SpecTemplateId: tplMotorId.toString(),
        Specs: [
          { Key: 'power', Label: '功率', Value: '200', Unit: 'kW' },
          { Key: 'voltage', Label: '电压', Value: '220', Unit: 'V' },
          { Key: 'efficiency', Label: '效率等级', Value: 'IE4', Unit: '' },
          { Key: 'ipRating', Label: '防护等级', Value: 'IP65', Unit: '' }
        ],
        TotalQty: 30,
        AvailableQty: 25,
        LockedQty: 5,
        CreatedAt: now,
        UpdatedAt: now
      },
      {
        _id: new ObjectId(),
        Name: '直流伺服电机 400W',
        Model: 'DMS-400',
        Description: '400W直流伺服电机，超高效率',
        Manufacturer: 'Delta',
        Brand: 'Delta',
        Category: '电机/伺服电机/直流伺服',
        Tags: ['伺服电机', '直流', '400W'],
        SpecTemplateId: tplMotorId.toString(),
        Specs: [
          { Key: 'power', Label: '功率', Value: '400', Unit: 'kW' },
          { Key: 'voltage', Label: '电压', Value: '220', Unit: 'V' },
          { Key: 'efficiency', Label: '效率等级', Value: 'IE4', Unit: '' },
          { Key: 'ipRating', Label: '防护等级', Value: 'IP67', Unit: '' }
        ],
        TotalQty: 20,
        AvailableQty: 18,
        LockedQty: 2,
        CreatedAt: now,
        UpdatedAt: now
      },
      {
        _id: new ObjectId(),
        Name: '直流伺服电机 750W',
        Model: 'DMS-750',
        Description: '750W直流伺服电机，工业级',
        Manufacturer: 'Delta',
        Brand: 'Delta',
        Category: '电机/伺服电机/直流伺服',
        Tags: ['伺服电机', '直流', '750W'],
        SpecTemplateId: tplMotorId.toString(),
        Specs: [
          { Key: 'power', Label: '功率', Value: '750', Unit: 'kW' },
          { Key: 'voltage', Label: '电压', Value: '380', Unit: 'V' },
          { Key: 'efficiency', Label: '效率等级', Value: 'IE3', Unit: '' },
          { Key: 'ipRating', Label: '防护等级', Value: 'IP66', Unit: '' }
        ],
        TotalQty: 15,
        AvailableQty: 12,
        LockedQty: 3,
        CreatedAt: now,
        UpdatedAt: now
      },
      // Stepper Motors
      {
        _id: new ObjectId(),
        Name: '两相步进电机 42mm',
        Model: 'SM-42',
        Description: '42mm两相混合式步进电机',
        Manufacturer: 'Mige',
        Brand: 'Mige',
        Category: '电机/步进电机/两相步进',
        Tags: ['步进电机', '两相', '42mm'],
        SpecTemplateId: tplMotorId.toString(),
        Specs: [
          { Key: 'power', Label: '功率', Value: '50', Unit: 'kW' },
          { Key: 'voltage', Label: '电压', Value: '24', Unit: 'V' },
          { Key: 'efficiency', Label: '效率等级', Value: 'IE2', Unit: '' },
          { Key: 'ipRating', Label: '防护等级', Value: 'IP65', Unit: '' }
        ],
        TotalQty: 80,
        AvailableQty: 70,
        LockedQty: 10,
        CreatedAt: now,
        UpdatedAt: now
      },
      {
        _id: new ObjectId(),
        Name: '两相步进电机 57mm',
        Model: 'SM-57',
        Description: '57mm两相混合式步进电机',
        Manufacturer: 'Mige',
        Brand: 'Mige',
        Category: '电机/步进电机/两相步进',
        Tags: ['步进电机', '两相', '57mm'],
        SpecTemplateId: tplMotorId.toString(),
        Specs: [
          { Key: 'power', Label: '功率', Value: '100', Unit: 'kW' },
          { Key: 'voltage', Label: '电压', Value: '48', Unit: 'V' },
          { Key: 'efficiency', Label: '效率等级', Value: 'IE1', Unit: '' },
          { Key: 'ipRating', Label: '防护等级', Value: 'IP65', Unit: '' }
        ],
        TotalQty: 60,
        AvailableQty: 55,
        LockedQty: 5,
        CreatedAt: now,
        UpdatedAt: now
      }
    ]

    await db.collection('parts').insertMany(parts)
    console.log(`Created ${parts.length} parts`)

    // ============================================
    // Summary
    // ============================================
    console.log('\n========== Seed Complete ==========')
    console.log(`Templates: ${templates.length}`)
    console.log(`Categories: 12`)
    console.log(`Parts: ${parts.length}`)
    console.log('\nCategory Hierarchy:')
    console.log('  电子元器件')
    console.log('    ├── 微控制器')
    console.log('    │   ├── ARM (模板: 控制器)')
    console.log('    │   └── ESP32 (模板: 控制器)')
    console.log('    └── 传感器')
    console.log('        └── 温度传感器 (模板: 传感器)')
    console.log('  电机')
    console.log('    ├── 伺服电机')
    console.log('    │   └── 直流伺服 (模板: 电机)')
    console.log('    └── 步进电机')
    console.log('        └── 两相步进 (模板: 电机)')

  } catch (error) {
    console.error('Error:', error.message)
    throw error
  } finally {
    await client.close()
    console.log('\nDisconnected from MongoDB')
  }
}

seed()
