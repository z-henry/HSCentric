
using System.Collections.Generic;

namespace HSCentric.Const
{
	enum R_UNIT_MENU
	{
		启用,
		更新,
		启动,
	}
	enum LIST_UNIT_COLUMN
	{
		启用,
		成员,
		当前模式,
		预设模式,
		唤醒时间,
		启用时间段,
		等级,
		经验,
		PVP分数,
		传统模式等级,
	}
	enum R_TASK_MENU
	{
		添加,
		修改,
		删除,
	}
	enum LIST_TASK_COLUMN
	{
		模式,
		队伍,
		策略,
		启动时间,
		停止时间,
		齿轮,
		地图,
		核心,
		总数,
	}

	public enum TASK_MODE
	{
		刷图,
		刷神秘人,
		全自动接任务做任务,
		自动解锁地图,
		Pvp,
		挂机收菜,
		自动解锁装备,
		自动主线,
		狂野,
		标准,
		经典,
		休闲,
	}

	public enum BEHAVIOR_MODE
	{
		通用_不设惩罚,
		经典_防战,
		经典_动物园,
		经典_咆哮德,
		狂野_酸奥秘法,
		狂野_酸剑鱼贼,
		通用_酸鱼人萨,
		标准_酸快攻德,
		过时_暗牧,
		过时_任务海盗战,
	}

	public enum FILE_TYPE
	{
		主进程日志,
		佣兵日志,
		佣兵对局日志,
		兄弟日志,
		Total,
	}

}
